import type { DeclarationStatus } from '@/api/types'

export function declarationStatusLabel(status: DeclarationStatus): string {
  switch (status) {
    case 'Completed':
      return 'Completed'
    case 'CompletedWithWarnings':
      return 'Completed with warnings'
    case 'Failed':
      return 'Failed'
  }
}

export function declarationStatusBadgeVariant(status: DeclarationStatus): 'success' | 'warning' | 'destructive' {
  switch (status) {
    case 'Completed':
      return 'success'
    case 'CompletedWithWarnings':
      return 'warning'
    case 'Failed':
      return 'destructive'
  }
}
