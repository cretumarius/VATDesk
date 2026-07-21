// Single shared formatting utility — skill hard rule #3: amounts via
// Intl.NumberFormat('hu-HU'), no decimals for HUF, tabular numerals in tables (the
// `.tabular-nums-mono` class, applied by callers). Never call toLocaleString or build
// separators by hand elsewhere — always go through these functions.

const hufAmountFormatter = new Intl.NumberFormat('hu-HU', { maximumFractionDigits: 0 })

/** Grouped digits only (hu-HU locale, space-separated), no currency symbol — callers add a "HUF" label where the design shows one. */
export function formatAmount(value: number): string {
  return hufAmountFormatter.format(value)
}

/** ISO date (yyyy-MM-dd) from an ISO timestamp, matching the design's plain date display. */
export function formatDate(isoTimestamp: string): string {
  return isoTimestamp.slice(0, 10)
}
