import { useMemo, useState } from 'react'

import type { Severity, ValidationIssueDto, ValidationSummaryDto } from '@/api/types'
import { Card } from '@/components/ui/card'
import { cn } from '@/lib/utils'

interface ValidationPanelProps {
  validation: ValidationSummaryDto
}

const SEVERITY_ORDER: Record<Severity, number> = { Error: 0, Warning: 1, Info: 2 }

const SEVERITY_STYLE: Record<Severity, { bg: string; fg: string; label: string }> = {
  Error: { bg: 'bg-destructive-bg', fg: 'text-destructive', label: 'ERROR' },
  Warning: { bg: 'bg-warning-bg', fg: 'text-warning', label: 'WARNING' },
  Info: { bg: 'bg-accent', fg: 'text-accent-foreground', label: 'INFO' },
}

function sortIssues(issues: ValidationIssueDto[]): ValidationIssueDto[] {
  return [...issues].sort((a, b) => SEVERITY_ORDER[a.severity] - SEVERITY_ORDER[b.severity] || a.rowNumber - b.rowNumber)
}

export function ValidationPanel({ validation }: ValidationPanelProps) {
  const [open, setOpen] = useState(true)
  const sortedIssues = useMemo(() => sortIssues(validation.issues), [validation.issues])

  return (
    <Card className="mb-5 overflow-hidden">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="flex w-full items-center gap-3.5 p-[15px_20px] text-left"
      >
        <span className="text-sm font-semibold">Validation</span>
        <div className="flex items-center gap-[7px]">
          <span className="rounded-full bg-success-bg px-[9px] py-[3px] text-[11.5px] font-semibold text-success">
            {validation.validRows} valid
          </span>
          <span className="rounded-full bg-warning-bg px-[9px] py-[3px] text-[11.5px] font-semibold text-warning">
            {validation.warningRows} warnings
          </span>
          <span className="rounded-full bg-destructive-bg px-[9px] py-[3px] text-[11.5px] font-semibold text-destructive">
            {validation.errorRows} error{validation.errorRows === 1 ? '' : 's'}
          </span>
        </div>
        <span className="ml-auto text-[13px] text-subtle-foreground">{open ? '▲' : '▼'}</span>
      </button>

      {open && (
        <div className="border-t border-divider">
          {sortedIssues.length === 0 ? (
            <div className="p-[20px] text-center text-sm text-muted-foreground">
              No validation issues — every row passed all checks.
            </div>
          ) : (
            sortedIssues.map((issue, index) => {
              const style = SEVERITY_STYLE[issue.severity]
              return (
                <div
                  key={`${issue.rowNumber}-${issue.ruleId}-${index}`}
                  className="flex items-start gap-[13px] border-b border-divider p-[13px_20px] last:border-b-0"
                >
                  <div
                    className={cn(
                      'flex size-[22px] shrink-0 items-center justify-center rounded-full text-xs font-bold',
                      style.bg,
                      style.fg,
                    )}
                  >
                    !
                  </div>
                  <div className="flex-1">
                    <div className="text-[13.5px] leading-relaxed text-foreground">{issue.message}</div>
                    <div className="tabular-nums-mono mt-0.5 text-[11.5px] text-subtle-foreground">
                      Row {issue.rowNumber} · {issue.ruleId}
                    </div>
                  </div>
                  <span className={cn('text-[11px] font-semibold tracking-wide uppercase', style.fg)}>
                    {style.label}
                  </span>
                </div>
              )
            })
          )}
        </div>
      )}
    </Card>
  )
}
