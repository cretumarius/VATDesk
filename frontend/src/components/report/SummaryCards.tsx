import type { DeclarationDto } from '@/api/types'
import { Card } from '@/components/ui/card'
import { formatAmount } from '@/lib/format'

interface SummaryCardsProps {
  declaration: DeclarationDto
}

function totalRows(declaration: DeclarationDto): number {
  return declaration.perCategory.reduce((sum, c) => sum + c.rowCount, 0)
}

export function SummaryCards({ declaration }: SummaryCardsProps) {
  const rows = totalRows(declaration)
  const categoryCount = declaration.perCategory.length
  // Design only shows the payable (positive) case. Reclaimable (negative) keeps the same
  // card treatment — sign comes naturally from Intl formatting, caption swaps — logged as
  // a Design gap since the mockup doesn't cover it.
  const reclaimable = declaration.netVatPayable < 0

  return (
    <div className="mb-5 grid grid-cols-1 gap-3.5 sm:grid-cols-2 lg:grid-cols-[1fr_1fr_1.5fr_1fr]">
      <Card className="p-[18px_20px]">
        <div className="mb-2.5 text-xs text-muted-foreground">Total Output VAT</div>
        <div className="text-[22px] font-semibold tracking-tight">
          <span className="mr-1.5 align-top text-xs font-medium text-muted-foreground">HUF</span>
          <span className="tabular-nums-mono">{formatAmount(declaration.totalOutputVat)}</span>
        </div>
        <div className="mt-1.5 text-[11.5px] text-subtle-foreground">Sales · collected</div>
      </Card>

      <Card className="p-[18px_20px]">
        <div className="mb-2.5 text-xs text-muted-foreground">Deductible Input VAT</div>
        <div className="text-[22px] font-semibold tracking-tight">
          <span className="mr-1.5 align-top text-xs font-medium text-muted-foreground">HUF</span>
          <span className="tabular-nums-mono">{formatAmount(declaration.totalDeductibleInputVat)}</span>
        </div>
        <div className="mt-1.5 text-[11.5px] text-subtle-foreground">Purchases · reclaimable</div>
      </Card>

      <Card className="border-primary-hover bg-gradient-to-br from-primary to-primary-hover p-[18px_22px] text-primary-foreground shadow-[0_12px_28px_-12px_rgba(15,118,110,0.6)]">
        <div className="mb-2.5 text-xs text-primary-foreground/75">Net VAT Payable</div>
        <div className="text-[30px] font-semibold tracking-tight">
          <span className="mr-1.5 align-top text-xs font-medium text-primary-foreground/75">HUF</span>
          <span className="tabular-nums-mono">{formatAmount(declaration.netVatPayable)}</span>
        </div>
        <div className="mt-1.5 text-[11.5px] text-primary-foreground/80">
          {reclaimable ? 'Output − Input · reclaimable from NAV' : 'Output − Input · due to NAV'}
        </div>
      </Card>

      <Card className="p-[18px_20px]">
        <div className="mb-2.5 text-xs text-muted-foreground">Rows processed</div>
        <div className="tabular-nums-mono text-[22px] font-semibold tracking-tight">{rows}</div>
        <div className="mt-1.5 text-[11.5px] text-subtle-foreground">
          across {categoryCount} {categoryCount === 1 ? 'category' : 'categories'}
        </div>
      </Card>
    </div>
  )
}
