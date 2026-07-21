import { Fragment, useMemo, useState } from 'react'

import type { CategoryTotalDto, Direction, VatCategoryDto } from '@/api/types'
import { formatAmount } from '@/lib/format'
import { cn } from '@/lib/utils'
import { categoryBadgeLabel, categoryDisplayName, directionLabel } from '@/lib/vat-category-display'

interface CategoryBreakdownTableProps {
  perCategory: CategoryTotalDto[]
  categories: VatCategoryDto[]
}

type DirectionFilter = 'All' | Direction

interface Totals {
  rowCount: number
  net: number
  vat: number
  gross: number
}

function sumTotals(rows: CategoryTotalDto[]): Totals {
  return rows.reduce(
    (acc, r) => ({
      rowCount: acc.rowCount + r.rowCount,
      net: acc.net + r.totalNet,
      vat: acc.vat + r.totalVat,
      gross: acc.gross + r.totalGross,
    }),
    { rowCount: 0, net: 0, vat: 0, gross: 0 },
  )
}

const FILTERS: DirectionFilter[] = ['All', 'Out', 'In']

function filterLabel(filter: DirectionFilter): string {
  if (filter === 'All') return 'All'
  return directionLabel(filter)
}

export function CategoryBreakdownTable({ perCategory, categories }: CategoryBreakdownTableProps) {
  const [filter, setFilter] = useState<DirectionFilter>('All')
  const categoryByCode = useMemo(() => new Map(categories.map((c) => [c.code, c])), [categories])

  // Backend already returns perCategory as Out block then In block, by registry SortOrder.
  const groups = (['Out', 'In'] as const)
    .map((direction) => ({ direction, rows: perCategory.filter((c) => c.direction === direction) }))
    .filter((g) => g.rows.length > 0)

  const visibleGroups = groups.filter((g) => filter === 'All' || g.direction === filter)
  const grandTotals = sumTotals(visibleGroups.flatMap((g) => g.rows))

  const exemptOrReverseChargeCategories = categories.filter((c) => c.rate === null)

  return (
    <div>
      <div className="mb-3.5 flex flex-wrap items-center justify-between gap-4">
        <h2 className="text-base font-semibold">Breakdown by VAT category</h2>
        <div className="flex gap-0.5 rounded-lg border border-input bg-card p-[3px]">
          {FILTERS.map((f) => (
            <button
              key={f}
              type="button"
              onClick={() => setFilter(f)}
              className={cn(
                'rounded-md px-3 py-1.5 text-[12.5px] font-medium',
                filter === f ? 'bg-accent text-primary' : 'text-muted-foreground hover:text-foreground',
              )}
            >
              {filterLabel(f)}
            </button>
          ))}
        </div>
      </div>

      <div className="overflow-hidden rounded-2xl border border-border bg-card shadow-[0_1px_2px_rgba(16,32,30,0.04)]">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[760px] border-collapse">
            <thead>
              <tr className="border-b border-border bg-muted">
                <th className="p-[11px_20px] text-left text-[11px] font-semibold tracking-wide text-table-header-foreground uppercase">
                  Category
                </th>
                <th className="p-[11px_16px] text-left text-[11px] font-semibold tracking-wide text-table-header-foreground uppercase">
                  Direction
                </th>
                <th className="p-[11px_16px] text-right text-[11px] font-semibold tracking-wide text-table-header-foreground uppercase">
                  Rows
                </th>
                <th className="p-[11px_16px] text-right text-[11px] font-semibold tracking-wide text-table-header-foreground uppercase">
                  Net (HUF)
                </th>
                <th className="p-[11px_16px] text-right text-[11px] font-semibold tracking-wide text-table-header-foreground uppercase">
                  VAT (HUF)
                </th>
                <th className="p-[11px_20px] text-right text-[11px] font-semibold tracking-wide text-table-header-foreground uppercase">
                  Gross (HUF)
                </th>
              </tr>
            </thead>
            <tbody>
              {visibleGroups.map((group) => {
                const subtotal = sumTotals(group.rows)
                const groupLabel = directionLabel(group.direction)
                return (
                  <Fragment key={group.direction}>
                    <tr className="bg-muted/60">
                      <td
                        colSpan={6}
                        className="border-b border-divider p-[9px_20px] text-[11px] font-semibold tracking-wide text-primary uppercase"
                      >
                        {groupLabel}
                      </td>
                    </tr>
                    {group.rows.map((row) => {
                      const category = categoryByCode.get(row.vatCode)
                      return (
                        <tr
                          key={`${group.direction}-${row.vatCode}`}
                          className="border-b border-divider hover:bg-muted/40"
                        >
                          <td className="p-3 pl-5">
                            <div className="flex items-center gap-2.5">
                              <span className="tabular-nums-mono inline-block min-w-[44px] rounded-md bg-accent px-2 py-0.5 text-center text-xs font-semibold text-primary">
                                {category ? categoryBadgeLabel(category) : row.vatCode}
                              </span>
                              <span className="text-[13px] text-foreground/85">
                                {category ? categoryDisplayName(category) : row.vatCode}
                              </span>
                            </div>
                          </td>
                          <td className="p-[12px_16px] text-[12.5px] text-muted-foreground">{groupLabel}</td>
                          <td className="tabular-nums-mono p-[12px_16px] text-right text-[13px] text-foreground/85">
                            {row.rowCount}
                          </td>
                          <td className="tabular-nums-mono p-[12px_16px] text-right text-[13px]">
                            HUF {formatAmount(row.totalNet)}
                          </td>
                          <td className="tabular-nums-mono p-[12px_16px] text-right text-[13px]">
                            HUF {formatAmount(row.totalVat)}
                          </td>
                          <td className="tabular-nums-mono p-[12px_20px] text-right text-[13px] font-medium">
                            HUF {formatAmount(row.totalGross)}
                          </td>
                        </tr>
                      )
                    })}
                    <tr className="border-b border-divider bg-muted">
                      <td className="p-[11px_20px] text-[12.5px] font-semibold">Subtotal · {groupLabel}</td>
                      <td />
                      <td className="tabular-nums-mono p-[11px_16px] text-right text-[12.5px] font-semibold">
                        {subtotal.rowCount}
                      </td>
                      <td className="tabular-nums-mono p-[11px_16px] text-right text-[12.5px] font-semibold">
                        HUF {formatAmount(subtotal.net)}
                      </td>
                      <td className="tabular-nums-mono p-[11px_16px] text-right text-[12.5px] font-semibold">
                        HUF {formatAmount(subtotal.vat)}
                      </td>
                      <td className="tabular-nums-mono p-[11px_20px] text-right text-[12.5px] font-semibold">
                        HUF {formatAmount(subtotal.gross)}
                      </td>
                    </tr>
                  </Fragment>
                )
              })}
              <tr className="bg-primary text-primary-foreground">
                <td className="p-[14px_20px] text-[13px] font-semibold">Total</td>
                <td />
                <td className="tabular-nums-mono p-[14px_16px] text-right text-[13px] font-semibold">
                  {grandTotals.rowCount}
                </td>
                <td className="tabular-nums-mono p-[14px_16px] text-right text-[13px] font-semibold">
                  HUF {formatAmount(grandTotals.net)}
                </td>
                <td className="tabular-nums-mono p-[14px_16px] text-right text-[13px] font-semibold">
                  HUF {formatAmount(grandTotals.vat)}
                </td>
                <td className="tabular-nums-mono p-[14px_20px] text-right text-[13.5px] font-bold">
                  HUF {formatAmount(grandTotals.gross)}
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      {exemptOrReverseChargeCategories.length > 0 && (
        <div className="mt-3 flex flex-wrap gap-4 text-[11.5px] text-subtle-foreground">
          {exemptOrReverseChargeCategories.map((c) => (
            <span key={c.code}>
              <b className="text-muted-foreground">{c.code}</b> {categoryDisplayName(c).toLowerCase()}
            </span>
          ))}
        </div>
      )}
    </div>
  )
}
