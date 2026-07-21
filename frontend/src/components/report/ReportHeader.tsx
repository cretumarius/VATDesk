import { useNavigate } from 'react-router-dom'

import type { DeclarationDto } from '@/api/types'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { declarationStatusBadgeVariant, declarationStatusLabel } from '@/lib/declaration-status'
import { formatDate } from '@/lib/format'

interface ReportHeaderProps {
  declaration: DeclarationDto
  onDownloadPdf: () => void
  downloadingPdf: boolean
}

export function ReportHeader({ declaration, onDownloadPdf, downloadingPdf }: ReportHeaderProps) {
  const navigate = useNavigate()

  return (
    <>
      <button
        type="button"
        onClick={() => navigate('/')}
        className="mb-4 text-[13px] text-muted-foreground hover:text-primary"
      >
        ← Back to dashboard
      </button>

      <div className="mb-6 flex flex-wrap items-start justify-between gap-[18px]">
        <div>
          <div className="mb-2 flex items-center gap-3">
            <h1 className="tabular-nums-mono text-[22px] font-semibold tracking-tight">{declaration.id}</h1>
            <Badge variant={declarationStatusBadgeVariant(declaration.status)}>
              {declarationStatusLabel(declaration.status)}
            </Badge>
          </div>
          <div className="flex flex-wrap gap-[18px] text-[13px] text-muted-foreground">
            <span>
              Source · <span className="tabular-nums-mono text-foreground">{declaration.sourceFilename}</span>
            </span>
            <span>
              Processed · <span className="tabular-nums-mono text-foreground">{formatDate(declaration.createdAt)}</span>
            </span>
          </div>
        </div>
        <div className="flex gap-2.5">
          <Button type="button" variant="outline" onClick={() => navigate('/')}>
            Back to dashboard
          </Button>
          <Button type="button" onClick={onDownloadPdf} disabled={downloadingPdf}>
            {downloadingPdf ? 'Preparing PDF…' : '↓ Download PDF'}
          </Button>
        </div>
      </div>
    </>
  )
}
