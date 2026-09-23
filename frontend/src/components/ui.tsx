import type { FormEvent, ReactNode } from 'react';

export function Button({ children, tone = 'primary', type = 'button', onClick, disabled }: { children: ReactNode; tone?: 'primary' | 'secondary' | 'danger'; type?: 'button' | 'submit'; onClick?: () => void; disabled?: boolean }) {
  return <button className={`button ${tone}`} type={type} onClick={onClick} disabled={disabled}>{children}</button>;
}

export function Card({ title, children, action }: { title?: string; children: ReactNode; action?: ReactNode }) {
  return <section className="card"><div className="card-header">{title && <h2>{title}</h2>}{action}</div>{children}</section>;
}

export function Field({ label, children }: { label: string; children: ReactNode }) {
  return <label className="field"><span>{label}</span>{children}</label>;
}

export function Modal({ title, children, onClose }: { title: string; children: ReactNode; onClose: () => void }) {
  return <div className="modal-backdrop" role="presentation" onMouseDown={onClose}><section className="modal" role="dialog" aria-modal="true" onMouseDown={(event) => event.stopPropagation()}><div className="modal-title"><h2>{title}</h2><button className="icon-button" onClick={onClose} aria-label="Fechar">×</button></div>{children}</section></div>;
}

export function EmptyState({ text }: { text: string }) { return <div className="empty-state">{text}</div>; }

export function Form({ children, onSubmit }: { children: ReactNode; onSubmit: (event: FormEvent<HTMLFormElement>) => void }) { return <form onSubmit={onSubmit}>{children}</form>; }
