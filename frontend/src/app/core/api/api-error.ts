import { HttpErrorResponse } from '@angular/common/http';

interface ProblemDetails {
  detail?: string;
  title?: string;
  code?: string;
}

export function apiErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    const problem = error.error as ProblemDetails | null;
    if (typeof problem?.detail === 'string' && problem.detail.trim()) {
      return problem.detail;
    }

    if (error.status === 0) {
      return 'Não foi possível conectar ao serviço. Verifique se ele está em execução.';
    }
  }

  return fallback;
}
