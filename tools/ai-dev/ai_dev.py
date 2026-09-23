"""Orquestrador linear de desenvolvimento assistido por IA do Family+.

O fluxo completo é deliberadamente explícito:

    Jira -> worktree -> agente estruturado -> Codex -> pipeline -> checkpoint
         -> revisão -> correção opcional -> pipeline -> push -> Pull Request

As integrações externas ficam bloqueadas por padrão. Para executar o fluxo,
defina AI_DEV_ALLOW_EXTERNAL=true em .env.ai-dev e forneça as credenciais e
configurações necessárias. O módulo nunca imprime tokens ou valores secretos.
"""

from __future__ import annotations

import json
import os
import re
import shutil
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Literal, Protocol

import requests
from requests.auth import HTTPBasicAuth

from pydantic import BaseModel, ConfigDict, ValidationError

ROOT = Path(__file__).resolve().parents[2]
CONFIG_FILE = ROOT / ".env.ai-dev"
PIPELINE_FILE = ROOT / ".ai" / "pipeline.json"
TASK_SCHEMA = ROOT / ".ai" / "schemas" / "task.schema.json"
REVIEW_SCHEMA = ROOT / ".ai" / "schemas" / "review.schema.json"
WORKTREES_DIR = ROOT / ".worktrees"


class OrchestrationError(RuntimeError):
    """Erro operacional seguro, sem incluir credenciais na mensagem."""


class QuestionsPending(OrchestrationError):
    """A análise encontrou perguntas que exigem decisão humana."""


class TaskModel(BaseModel):
    model_config = ConfigDict(extra="forbid")

    task_id: str
    objective: str
    requirements: list[str]
    business_rules: list[str]
    acceptance_criteria: list[str]
    affected_areas: list[str]
    risks: list[str]
    questions: list[str]
    implementation_plan: list[str]


class FindingModel(BaseModel):
    model_config = ConfigDict(extra="forbid")

    severity: Literal["low", "medium", "high", "critical"]
    file: str
    description: str
    reason: str


class ReviewModel(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: Literal["approved", "changes_requested", "blocked"]
    findings: list[FindingModel]


@dataclass(frozen=True)
class Worktree:
    ticket: str
    branch: str
    path: Path
    run_dir: Path

    @property
    def issue_file(self) -> Path:
        return self.run_dir / "issue.json"

    @property
    def task_file(self) -> Path:
        return self.run_dir / "task.json"

    @property
    def review_file(self) -> Path:
        return self.run_dir / "review.json"

    @property
    def diff_file(self) -> Path:
        return self.run_dir / "diff.txt"

    @property
    def pr_file(self) -> Path:
        return self.run_dir / "pr.md"


def load_config() -> dict[str, str]:
    values: dict[str, str] = {
        key: value
        for key, value in os.environ.items()
        if key.startswith("JIRA_") or key.startswith("AI_DEV_")
    }
    if not CONFIG_FILE.exists():
        return values
    for raw_line in CONFIG_FILE.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        normalized_key = key.strip()
        if normalized_key not in values:
            values[normalized_key] = value.strip().strip('"').strip("'")
    return values


def mark(name: str, ok: bool, detail: str = "") -> bool:
    state = "OK" if ok else "PENDENTE"
    suffix = f" — {detail}" if detail else ""
    print(f"{name:<18} {state}{suffix}")
    return ok


def announce(step: int, title: str) -> None:
    print(f"\n[{step}/10] {title}")


def checkmark(detail: str) -> None:
    try:
        print(f"✓ {detail}")
    except UnicodeEncodeError:
        print(f"[OK] {detail}")


def run_command(
    command: list[str] | str,
    *,
    cwd: Path,
    capture: bool = False,
    check: bool = True,
) -> subprocess.CompletedProcess[str]:
    result = subprocess.run(
        command,
        cwd=cwd,
        shell=isinstance(command, str),
        text=True,
        capture_output=capture,
        check=False,
    )
    if check and result.returncode:
        detail = result.stderr.strip() if capture and result.stderr else "comando falhou"
        raise OrchestrationError(detail[:500])
    return result


def require_external(config: dict[str, str]) -> None:
    if config.get("AI_DEV_ALLOW_EXTERNAL", "").lower() != "true":
        raise OrchestrationError(
            "execução externa bloqueada; defina AI_DEV_ALLOW_EXTERNAL=true após revisar o fluxo"
        )


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


def read_context(workspace: Worktree, relative_paths: list[str]) -> str:
    sections: list[str] = []
    for relative_path in relative_paths:
        path = workspace.path / relative_path
        if not path.exists():
            continue
        sections.append(
            f"\n--- {relative_path} ---\n{path.read_text(encoding='utf-8')}\n"
        )
    return "".join(sections)


def jira_read_check(config: dict[str, str]) -> tuple[bool, str]:
    required = ("JIRA_CLOUD_ID", "JIRA_EMAIL", "JIRA_API_TOKEN", "JIRA_PROJECT")
    if any(not config.get(key) for key in required):
        return False, "configuração Jira incompleta"
    issue_key = f"{config['JIRA_PROJECT']}-18"
    base = f"https://api.atlassian.com/ex/jira/{config['JIRA_CLOUD_ID']}/rest/api/3"
    try:
        response = requests.get(
            f"{base}/issue/{issue_key}",
            auth=HTTPBasicAuth(config["JIRA_EMAIL"], config["JIRA_API_TOKEN"]),
            params={"fields": "summary"},
            timeout=20,
        )
        response.raise_for_status()
        return True, f"leitura de {issue_key} autorizada"
    except requests.HTTPError as error:
        return False, f"Jira respondeu HTTP {error.response.status_code}"
    except requests.RequestException:
        return False, "não foi possível conectar ao Jira"


class JiraClient:
    def __init__(self, config: dict[str, str]) -> None:
        self.config = config

    def get_issue(self, ticket: str) -> dict[str, Any]:
        required = ("JIRA_CLOUD_ID", "JIRA_EMAIL", "JIRA_API_TOKEN")
        if any(not self.config.get(key) for key in required):
            raise OrchestrationError("configuração Jira incompleta")
        if not re.fullmatch(r"[A-Za-z][A-Za-z0-9]+-\d+", ticket):
            raise OrchestrationError("ticket inválido; use o formato PROJ-123")
        base = (
            f"https://api.atlassian.com/ex/jira/{self.config['JIRA_CLOUD_ID']}"
            "/rest/api/3"
        )
        try:
            response = requests.get(
                f"{base}/issue/{ticket}",
                auth=HTTPBasicAuth(
                    self.config["JIRA_EMAIL"], self.config["JIRA_API_TOKEN"]
                ),
                params={
                    "fields": (
                        "summary,description,status,priority,labels,issuetype"
                    )
                },
                timeout=30,
            )
            response.raise_for_status()
            return response.json()
        except requests.HTTPError as error:
            raise OrchestrationError(
                f"Jira respondeu HTTP {error.response.status_code}"
            ) from error
        except (requests.RequestException, ValueError) as error:
            raise OrchestrationError("não foi possível ler o ticket no Jira") from error


class GitWorktree:
    def __init__(self, config: dict[str, str]) -> None:
        self.config = config

    @staticmethod
    def slug(ticket: str) -> str:
        return re.sub(r"[^A-Za-z0-9._-]+", "-", ticket).strip("-")

    def base_ref(self) -> tuple[str, str, str]:
        remote = self.config.get("GIT_REMOTE", "origin")
        base_branch = self.config.get("BASE_BRANCH", "develop")
        return remote, base_branch, f"{remote}/{base_branch}"

    def fetch_base(self) -> None:
        remote, base_branch, base_ref = self.base_ref()
        remotes = run_command(["git", "remote"], cwd=ROOT, capture=True).stdout.split()
        if remote not in remotes:
            raise OrchestrationError(
                f"remote '{remote}' não está configurado; configure o origin antes do run"
            )
        run_command(["git", "fetch", remote, base_branch], cwd=ROOT)
        base_check = run_command(
            ["git", "show-ref", "--verify", "--quiet", f"refs/remotes/{base_ref}"],
            cwd=ROOT,
            check=False,
        )
        if base_check.returncode:
            raise OrchestrationError(f"base remota não encontrada: {base_ref}")

    def create_worktree(self, ticket: str) -> Worktree:
        slug = self.slug(ticket)
        branch = f"ai/{slug}"
        _, _, base_ref = self.base_ref()
        configured_root = self.config.get("WORKTREE_ROOT", "")
        worktree_root = (
            Path(configured_root)
            if configured_root
            else ROOT.parent / f"{ROOT.name}-worktrees"
        )
        path = worktree_root / slug
        branch_check = run_command(
            ["git", "show-ref", "--verify", "--quiet", f"refs/heads/{branch}"],
            cwd=ROOT,
            check=False,
        )
        if branch_check.returncode == 0:
            raise OrchestrationError(f"branch local já existe: {branch}")
        if path.exists():
            raise OrchestrationError(f"worktree já existe: {path}")
        worktree_root.mkdir(parents=True, exist_ok=True)
        run_command(
            ["git", "worktree", "add", "-b", branch, str(path), base_ref],
            cwd=ROOT,
        )
        run_dir = path / ".ai-run" / slug
        run_dir.mkdir(parents=True, exist_ok=True)
        return Worktree(ticket=ticket, branch=branch, path=path, run_dir=run_dir)

    def find_worktree(self, ticket: str) -> Worktree:
        slug = self.slug(ticket)
        branch = f"ai/{slug}"
        result = run_command(
            ["git", "worktree", "list", "--porcelain"],
            cwd=ROOT,
            capture=True,
        )
        current_path: Path | None = None
        for line in result.stdout.splitlines():
            if line.startswith("worktree "):
                current_path = Path(line.split(" ", 1)[1])
            elif line == f"branch refs/heads/{branch}" and current_path:
                run_dir = current_path / ".ai-run" / slug
                return Worktree(
                    ticket=ticket,
                    branch=branch,
                    path=current_path,
                    run_dir=run_dir,
                )
        raise OrchestrationError(f"worktree não encontrado para {ticket}: {branch}")

    def cleanup(self, worktree: Worktree) -> None:
        status = run_command(
            ["git", "status", "--porcelain"],
            cwd=worktree.path,
            capture=True,
        )
        if status.stdout.strip():
            raise OrchestrationError(
                "worktree possui alterações; commit ou descarte-as antes do cleanup"
            )
        remote, base_branch, base_ref = self.base_ref()
        run_command(["git", "fetch", remote, base_branch], cwd=ROOT)
        merged = run_command(
            ["git", "merge-base", "--is-ancestor", worktree.branch, base_ref],
            cwd=ROOT,
            check=False,
        )
        if merged.returncode:
            raise OrchestrationError(
                f"branch {worktree.branch} ainda não está integrada em {base_ref}"
            )
        run_command(["git", "worktree", "remove", str(worktree.path)], cwd=ROOT)

    def checkpoint(self, worktree: Worktree, message: str) -> None:
        run_command(["git", "diff", "--check"], cwd=worktree.path)
        run_command(["git", "add", "-A"], cwd=worktree.path)
        staged = run_command(
            ["git", "diff", "--cached", "--quiet"],
            cwd=worktree.path,
            check=False,
        )
        if staged.returncode == 0:
            return
        run_command(["git", "commit", "-m", message], cwd=worktree.path)

    def diff_against_base(self, worktree: Worktree) -> str:
        remote = self.config.get("GIT_REMOTE", "origin")
        base_branch = self.config.get("BASE_BRANCH", "develop")
        result = run_command(
            ["git", "diff", f"{remote}/{base_branch}...HEAD"],
            cwd=worktree.path,
            capture=True,
        )
        worktree.diff_file.write_text(result.stdout, encoding="utf-8")
        return result.stdout

    def push(self, worktree: Worktree) -> None:
        run_command(["git", "push", "-u", "origin", worktree.branch], cwd=worktree.path)


class StructuredAgent(Protocol):
    def analyze(self, issue: dict[str, Any], workspace: Worktree) -> TaskModel: ...

    def review(self, task: TaskModel, workspace: Worktree) -> ReviewModel: ...


class CodexAgent(Protocol):
    def implement(self, task: TaskModel, workspace: Worktree) -> None: ...

    def fix(self, review: ReviewModel, workspace: Worktree) -> None: ...


def _structured_payload(raw: str) -> dict[str, Any]:
    candidates: list[Any] = []
    try:
        candidates.append(json.loads(raw))
    except json.JSONDecodeError:
        for line in raw.splitlines():
            try:
                candidates.append(json.loads(line))
            except json.JSONDecodeError:
                continue
    for candidate in reversed(candidates):
        if not isinstance(candidate, dict):
            continue
        structured = candidate.get("structured_output")
        if isinstance(structured, dict):
            return structured
        response = candidate.get("response")
        if isinstance(response, dict):
            return response
        if isinstance(response, str) and response.strip():
            try:
                nested = json.loads(response)
            except json.JSONDecodeError:
                continue
            if isinstance(nested, dict):
                return nested
        if all(key in candidate for key in ("task_id", "objective")):
            return candidate
    raise OrchestrationError(
        "agente estruturado não retornou resposta JSON; verifique denied_actions e o prompt"
    )


class AgyAgent:
    def __init__(self, config: dict[str, str]) -> None:
        self.config = config

    def _run(
        self,
        prompt: str,
        schema: Path,
        workspace: Worktree,
        output_file: Path,
    ) -> dict[str, Any]:
        command = [
            "agy",
            "-p",
            prompt,
            "--output-format",
            "json",
            "--json-schema",
            str(schema),
            "--sandbox",
        ]
        result = run_command(command, cwd=workspace.path, capture=True)
        payload = _structured_payload(result.stdout)
        write_json(output_file, payload)
        return payload

    def analyze(self, issue: dict[str, Any], workspace: Worktree) -> TaskModel:
        write_json(workspace.issue_file, issue)
        context = read_context(
            workspace,
            [
                "AGENTS.md",
                "docs/business-rules.md",
                "docs/architecture.md",
                "docs/database.md",
            ],
        )
        prompt = (
            "Analise o ticket usando o contexto textual fornecido abaixo. "
            "Não execute comandos, não use ferramentas e não altere arquivos. "
            "Responda diretamente com um único objeto JSON que cumpra o schema.\n"
            f"TICKET:\n{json.dumps(issue, ensure_ascii=False, indent=2)}\n"
            f"CONTEXTO:\n{context}"
        )
        try:
            return TaskModel.model_validate(
                self._run(prompt, TASK_SCHEMA, workspace, workspace.task_file)
            )
        except ValidationError as error:
            raise OrchestrationError("análise estruturada não atende task.schema.json") from error

    def review(self, task: TaskModel, workspace: Worktree) -> ReviewModel:
        context = read_context(
            workspace,
            [
                "AGENTS.md",
                "docs/business-rules.md",
                "docs/architecture.md",
                "docs/database.md",
            ],
        )
        diff = (
            workspace.diff_file.read_text(encoding="utf-8")
            if workspace.diff_file.exists()
            else ""
        )
        prompt = (
            "Revise a implementação usando o contexto textual fornecido abaixo. "
            "Não execute comandos, não use ferramentas e não altere arquivos. "
            "Responda diretamente com um único objeto JSON que cumpra o schema.\n"
            f"TASK:\n{task.model_dump_json(indent=2)}\n"
            f"DIFF:\n{diff}\n"
            f"CONTEXTO:\n{context}"
        )
        try:
            return ReviewModel.model_validate(
                self._run(prompt, REVIEW_SCHEMA, workspace, workspace.review_file)
            )
        except ValidationError as error:
            raise OrchestrationError("revisão estruturada não atende review.schema.json") from error


class CodexCliAgent:
    def _run(self, prompt: str, workspace: Worktree) -> None:
        run_command(
            [
                "codex",
                "exec",
                "-s",
                "workspace-write",
                "-C",
                str(workspace.path),
                prompt,
            ],
            cwd=workspace.path,
        )

    def implement(self, task: TaskModel, workspace: Worktree) -> None:
        self._run(
            "Leia AGENTS.md e "
            f"{workspace.task_file.relative_to(workspace.path)}. "
            "Implemente a tarefa no workspace. Não faça commit. Não faça push. "
            "Execute verificações locais adequadas.",
            workspace,
        )

    def fix(self, review: ReviewModel, workspace: Worktree) -> None:
        write_json(workspace.review_file, review.model_dump())
        self._run(
            "Leia AGENTS.md, "
            f"{workspace.task_file.relative_to(workspace.path)} e "
            f"{workspace.review_file.relative_to(workspace.path)}. "
            "Corrija os findings no workspace. Não faça commit. Não faça push. "
            "Execute verificações locais adequadas.",
            workspace,
        )


class PipelineRunner:
    @staticmethod
    def label(command: str) -> str:
        if "dotnet restore" in command:
            return "restore"
        if "dotnet build" in command:
            return "build"
        if "dotnet test" in command:
            return "tests"
        if "npm ci" in command:
            return "npm ci"
        if "npm run lint" in command:
            return "lint"
        if "npm run build" in command:
            return "frontend build"
        return command

    def run(self, worktree: Worktree) -> None:
        if not PIPELINE_FILE.exists():
            raise OrchestrationError("pipeline não encontrado")
        try:
            commands = json.loads(PIPELINE_FILE.read_text(encoding="utf-8"))["commands"]
        except (KeyError, json.JSONDecodeError) as error:
            raise OrchestrationError("pipeline inválido") from error
        for command in commands:
            print(f"\n$ {command}")
            try:
                run_command(command, cwd=worktree.path)
            except OrchestrationError:
                print(f"PIPELINE FAILED: {command}")
                raise
            checkmark(self.label(command))
        print("\nPIPELINE PASSED")
        (worktree.run_dir / "pipeline.ok").write_text("passed\n", encoding="utf-8")


class GitHubClient:
    def __init__(self, config: dict[str, str]) -> None:
        self.config = config

    def create_pull_request(
        self,
        worktree: Worktree,
        task: TaskModel,
        review: ReviewModel,
    ) -> None:
        base_branch = self.config.get("BASE_BRANCH", "develop")
        title = f"{task.task_id} - Implementação"
        body = "\n".join(
            [
                f"# {task.task_id}",
                "",
                "## Implementação",
                "- backend",
                "- frontend",
                "- testes",
                "",
                "## Verificações",
                "- pipeline: PASS",
                "",
                "## Review Google",
                f"- status: {review.status}",
                f"- findings: {len(review.findings)}",
                "",
                "## Status",
                "Aguardando revisão humana.",
            ]
        )
        worktree.pr_file.write_text(body + "\n", encoding="utf-8")
        run_command(
            [
                "gh",
                "pr",
                "create",
                "--base",
                base_branch,
                "--head",
                worktree.branch,
                "--title",
                title,
                "--body-file",
                str(worktree.pr_file),
            ],
            cwd=worktree.path,
        )
        (worktree.run_dir / "pr.created").write_text("created\n", encoding="utf-8")


def load_task(worktree: Worktree) -> TaskModel:
    if not worktree.task_file.exists():
        raise OrchestrationError(f"task.json não encontrado: {worktree.task_file}")
    try:
        return TaskModel.model_validate_json(worktree.task_file.read_text(encoding="utf-8"))
    except (OSError, ValidationError) as error:
        raise OrchestrationError("task.json inválido") from error


def plan(ticket: str) -> int:
    """Busca Jira e gera task.json sem criar worktree ou alterar código."""
    config = load_config()
    require_external(config)
    announce(1, "Jira")
    issue = JiraClient(config).get_issue(ticket)
    checkmark(f"{ticket} encontrada")
    announce(2, "Google Analysis")
    slug = GitWorktree.slug(ticket)
    workspace = Worktree(
        ticket=ticket,
        branch="plan",
        path=ROOT,
        run_dir=ROOT / ".ai-run" / slug,
    )
    task = AgyAgent(config).analyze(issue, workspace)
    checkmark("task.json validado")
    if task.questions:
        print("Perguntas pendentes:")
        for question in task.questions:
            print(f"- {question}")
    print(f"\nPLAN READY: {workspace.task_file}")
    return 0


def review_ticket(ticket: str) -> int:
    """Revisa o diff de um worktree existente sem alterar código."""
    config = load_config()
    require_external(config)
    git = GitWorktree(config)
    worktree = git.find_worktree(ticket)
    task = load_task(worktree)
    git.diff_against_base(worktree)
    announce(1, "Google Review")
    review = AgyAgent(config).review(task, worktree)
    print(f"Status: {review.status}")
    print(f"Findings: {len(review.findings)}")
    print(f"Review: {worktree.review_file}")
    return 0 if review.status == "approved" else 1


def test_ticket(ticket: str | None = None) -> int:
    """Executa o pipeline na raiz ou no worktree do ticket."""
    config = load_config()
    git = GitWorktree(config)
    worktree = git.find_worktree(ticket) if ticket else Worktree(
        ticket="root",
        branch="root",
        path=ROOT,
        run_dir=ROOT / ".ai-run" / "root",
    )
    worktree.run_dir.mkdir(parents=True, exist_ok=True)
    PipelineRunner().run(worktree)
    return 0


def cleanup_ticket(ticket: str) -> int:
    """Remove o worktree limpo, sem apagar a branch de trabalho."""
    config = load_config()
    if config.get("AI_DEV_ALLOW_CLEANUP", "").lower() != "true":
        raise OrchestrationError(
            "cleanup bloqueado; defina AI_DEV_ALLOW_CLEANUP=true após o merge"
        )
    worktree = GitWorktree(config).find_worktree(ticket)
    GitWorktree(config).cleanup(worktree)
    print(f"Worktree removido: {worktree.path}")
    return 0


def run(ticket: str) -> int:
    """Executa o fluxo linear completo para um ticket."""
    config = load_config()
    require_external(config)
    jira = JiraClient(config)
    git = GitWorktree(config)
    structured = AgyAgent(config)
    codex = CodexCliAgent()
    pipeline = PipelineRunner()
    github = GitHubClient(config)

    announce(1, "Jira")
    issue = jira.get_issue(ticket)
    checkmark(f"{ticket} encontrada")

    announce(2, "Git")
    git.fetch_base()
    _, base_branch, _ = git.base_ref()
    checkmark(f"{base_branch} atualizado")

    announce(3, "Worktree")
    worktree = git.create_worktree(ticket)
    checkmark(worktree.branch)

    announce(4, "Google Analysis")
    task = structured.analyze(issue, worktree)
    if task.questions:
        raise QuestionsPending(
            "análise exige decisão humana: " + " | ".join(task.questions)
        )
    checkmark("task.json validado")

    announce(5, "OpenAI Codex")
    codex.implement(task, worktree)
    checkmark("implementação concluída")

    announce(6, "Local Validation")
    pipeline.run(worktree)
    checkmark("pipeline aprovado")
    git.checkpoint(worktree, f"feat({ticket}): implementação inicial")
    git.diff_against_base(worktree)

    announce(7, "Google Review")
    review = structured.review(task, worktree)
    if review.status == "blocked":
        raise OrchestrationError("revisão bloqueou a implementação")
    correction_requested = review.status == "changes_requested"
    if correction_requested:
        checkmark(f"{len(review.findings)} finding(s) encontrado(s)")
    else:
        checkmark("review aprovado")

    announce(8, "OpenAI Correction")
    if correction_requested:
        codex.fix(review, worktree)
        checkmark("finding(s) corrigido(s)")
    else:
        checkmark("nenhuma correção solicitada")

    announce(9, "Final Validation")
    pipeline.run(worktree)
    if correction_requested:
        git.checkpoint(worktree, f"fix({ticket}): aplica findings da revisão")
    git.diff_against_base(worktree)
    review = structured.review(task, worktree)
    if review.status != "approved":
        raise OrchestrationError(f"revisão final não aprovada: {review.status}")
    checkmark("review final aprovado")

    announce(10, "GitHub")
    if config.get("AI_DEV_ALLOW_PUSH", "").lower() != "true":
        raise OrchestrationError(
            "push bloqueado; defina AI_DEV_ALLOW_PUSH=true após revisar o checkpoint"
        )
    git.push(worktree)
    if config.get("AI_DEV_ALLOW_PR", "").lower() != "true":
        raise OrchestrationError(
            "criação de PR bloqueada; defina AI_DEV_ALLOW_PR=true após revisar o push"
        )
    github.create_pull_request(worktree, task, review)
    checkmark("PR criado")
    print("\nSTATUS")
    print("READY FOR HUMAN REVIEW")
    return 0


def doctor() -> int:
    print("AI-DEV DOCTOR — Family+")
    print()
    checks: list[bool] = []
    for label, command in (
        ("Git", "git"),
        ("GitHub", "gh"),
        ("Codex", "codex"),
        ("Antigravity", "agy"),
        ("Python", "python"),
        ("Node", "node"),
        (".NET", "dotnet"),
    ):
        checks.append(mark(label, shutil.which(command) is not None))
    config = load_config()
    checks.append(mark("Arquivo .env.ai-dev", CONFIG_FILE.exists()))
    checks.append(mark("Pipeline", PIPELINE_FILE.exists()))
    checks.append(mark("Schema task", TASK_SCHEMA.exists()))
    checks.append(mark("Schema review", REVIEW_SCHEMA.exists()))
    jira_ok, jira_detail = jira_read_check(config)
    checks.append(mark("Jira leitura", jira_ok, jira_detail))
    return 0 if all(checks) else 1


def pipeline_test() -> int:
    if not PIPELINE_FILE.exists():
        print("Pipeline não encontrado.")
        return 1
    commands = json.loads(PIPELINE_FILE.read_text(encoding="utf-8")).get("commands", [])
    for command in commands:
        print(f"\n$ {command}")
        result = subprocess.run(command, cwd=ROOT, shell=True, check=False)
        if result.returncode:
            print(f"PIPELINE FAILED: {command}")
            return result.returncode
    print("\nPIPELINE PASSED")
    return 0


def status(ticket: str | None = None) -> int:
    config = load_config()
    if ticket:
        worktree = GitWorktree(config).find_worktree(ticket)
        print(ticket)
        print(f"Branch: {worktree.branch}")
        print(
            f"Google: {'complete' if worktree.task_file.exists() and worktree.review_file.exists() else 'pending'}"
        )
        print(f"Codex: {'complete' if worktree.diff_file.exists() else 'pending'}")
        print(f"CI: {'pass' if (worktree.run_dir / 'pipeline.ok').exists() else 'pending'}")
        print(f"PR: {'created' if (worktree.run_dir / 'pr.created').exists() else 'pending'}")
        print(
            "Status: Human Review"
            if (worktree.run_dir / "pr.created").exists()
            else "Status: In Progress"
        )
        return 0
    result = subprocess.run(
        ["git", "branch", "--show-current"],
        cwd=ROOT,
        text=True,
        capture_output=True,
        check=False,
    )
    branch = result.stdout.strip() or "sem branch"
    print("AI-DEV STATUS")
    print(f"Projeto: {config.get('JIRA_PROJECT', 'não configurado')}")
    print(f"Branch: {branch}")
    print(f"Jira: {'configurado' if config.get('JIRA_API_TOKEN') else 'pendente'}")
    print(f"Pipeline: {'configurado' if PIPELINE_FILE.exists() else 'pendente'}")
    return 0


def main() -> int:
    command = sys.argv[1] if len(sys.argv) > 1 else "doctor"
    argument = sys.argv[2] if len(sys.argv) > 2 else ""
    try:
        if command == "doctor":
            return doctor()
        if command == "plan" and argument:
            return plan(argument)
        if command == "review" and argument:
            return review_ticket(argument)
        if command == "test":
            return test_ticket(argument or None)
        if command == "status":
            return status(argument or None)
        if command == "run" and argument:
            return run(argument)
        if command == "cleanup" and argument:
            return cleanup_ticket(argument)
    except OrchestrationError as error:
        print(f"AI-DEV BLOQUEADO: {error}")
        return 1
    print(
        "Comandos disponíveis: doctor, plan <TICKET>, run <TICKET>, review <TICKET>, "
        "test [TICKET], status [TICKET], cleanup <TICKET>"
    )
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
