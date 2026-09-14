interface ErrorAlertProps {
  message?: string | null
}

export default function ErrorAlert({ message }: ErrorAlertProps) {
  if (!message) {
    return null
  }
  return <div className="alert alert-error">{message}</div>
}

export function errorMessage(error: unknown): string {
  if (error instanceof Error) {
    return error.message
  }
  return 'Ocurrió un error inesperado.'
}
