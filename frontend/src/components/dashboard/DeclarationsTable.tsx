import { useState } from 'react'
import { useNavigate } from 'react-router-dom'

import { downloadDeclarationPdf } from '@/api/client'
import type { DeclarationListItemDto } from '@/api/types'
import { Badge } from '@/components/ui/badge'
import { declarationStatusBadgeVariant, declarationStatusLabel } from '@/lib/declaration-status'
import { formatAmount, formatDate } from '@/lib/format'
import { useToast } from '@/context/ToastContext'

interface DeclarationsTableProps {
  declarations: DeclarationListItemDto[]
}

function sourceExtLabel(format: DeclarationListItemDto['sourceFormat']): string {
  return format === 'Csv' ? 'CSV' : 'XML'
}

export function DeclarationsTable({ declarations }: DeclarationsTableProps) {
  const navigate = useNavigate()
  const { addToast } = useToast()
  const [downloadingId, setDownloadingId] = useState<string | null>(null)

  async function handleDownloadPdf(id: string) {
    setDownloadingId(id)
    try {
      await downloadDeclarationPdf(id)
    } catch {
      addToast('error', 'PDF download failed', 'Could not generate the PDF. Please try again.')
    } finally {
      setDownloadingId(null)
    }
  }

  return (
    <div className="overflow-hidden rounded-2xl border border-border bg-card shadow-[0_1px_2px_rgba(16,32,30,0.04)]">
      <div className="overflow-x-auto">
        <table className="w-full min-w-[820px] border-collapse">
          <thead>
            <tr className="border-b border-border bg-muted">
              <th className="p-[12px_20px] text-left text-[11px] font-semibold tracking-wide text-table-header-foreground uppercase">
                Date
              </th>
              <th className="p-[12px_16px] text-left text-[11px] font-semibold tracking-wide text-table-header-foreground uppercase">
                Source file
              </th>
              <th className="p-[12px_16px] text-left text-[11px] font-semibold tracking-wide text-table-header-foreground uppercase">
                Status
              </th>
              <th className="p-[12px_16px] text-right text-[11px] font-semibold tracking-wide text-table-header-foreground uppercase">
                Net VAT payable
              </th>
              <th className="p-[12px_16px] text-left text-[11px] font-semibold tracking-wide text-table-header-foreground uppercase">
                Created by
              </th>
              <th className="p-[12px_20px] text-right text-[11px] font-semibold tracking-wide text-table-header-foreground uppercase">
                Actions
              </th>
            </tr>
          </thead>
          <tbody>
            {declarations.map((d) => (
              <tr key={d.id} className="border-b border-divider hover:bg-muted/40">
                <td className="tabular-nums-mono p-[15px_20px] text-[13px] whitespace-nowrap text-foreground/85">
                  {formatDate(d.createdAt)}
                </td>
                <td className="p-[15px_16px]">
                  <div className="flex items-center gap-2.5">
                    <span className="rounded-md bg-secondary px-1.5 py-0.5 text-[11px] font-semibold text-secondary-foreground uppercase">
                      {sourceExtLabel(d.sourceFormat)}
                    </span>
                    <span className="tabular-nums-mono text-[13px] text-foreground">{d.sourceFilename}</span>
                  </div>
                  <div className="tabular-nums-mono mt-0.5 text-[11px] text-subtle-foreground">{d.id}</div>
                </td>
                <td className="p-[15px_16px]">
                  <Badge variant={declarationStatusBadgeVariant(d.status)}>{declarationStatusLabel(d.status)}</Badge>
                </td>
                <td className="tabular-nums-mono p-[15px_16px] text-right text-[13.5px] font-medium whitespace-nowrap text-foreground">
                  HUF {formatAmount(d.netVatPayable)}
                </td>
                <td className="p-[15px_16px] text-[13px] whitespace-nowrap text-foreground/85">
                  {d.createdByName ?? '—'}
                </td>
                <td className="p-[15px_20px]">
                  <div className="flex justify-end gap-[7px]">
                    <button
                      type="button"
                      onClick={() => navigate(`/declarations/${d.id}`)}
                      className="rounded-lg border border-input px-3.5 py-1.5 text-[12.5px] font-medium text-foreground hover:border-primary hover:text-primary"
                    >
                      View
                    </button>
                    <button
                      type="button"
                      onClick={() => handleDownloadPdf(d.id)}
                      disabled={downloadingId === d.id}
                      className="rounded-lg border border-input px-3.5 py-1.5 text-[12.5px] font-medium text-foreground hover:border-primary hover:text-primary disabled:opacity-50"
                    >
                      {downloadingId === d.id ? '…' : 'PDF'}
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
