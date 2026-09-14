interface StatusBadgeProps {
  estado: string
}

const VARIANTS: Record<string, string> = {
  Pendiente: 'badge-warning',
  Completada: 'badge-success',
  Cancelada: 'badge-danger',
}

export default function StatusBadge({ estado }: StatusBadgeProps) {
  const variant = VARIANTS[estado] ?? 'badge-neutral'
  return <span className={`badge ${variant}`}>{estado}</span>
}
