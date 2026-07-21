import type { Direction, VatCategoryDto } from '@/api/types'

/** "27%" / "0%" for rated categories (Percentage or ZeroRated, both carry a numeric rate); the bare code otherwise (AAM, TAM, EUFAD, FAD). Driven by the registry's `rate` field, never hardcoded per-code. */
export function categoryBadgeLabel(category: VatCategoryDto): string {
  return category.rate !== null ? `${category.code}%` : category.code
}

/**
 * The registry's DisplayNameEn for rated categories already ends with a parenthetical
 * rate (e.g. "Standard rate (27%)"); the badge next to it shows the same rate, so the
 * suffix is redundant on screen and is dropped here. Purely a display trim — the
 * canonical text still always comes from the registry API, nothing is hardcoded.
 */
export function categoryDisplayName(category: VatCategoryDto): string {
  return category.displayNameEn.replace(/\s*\(\d+%\)$/, '')
}

export function directionLabel(direction: Direction): string {
  return direction === 'Out' ? 'Sales' : 'Purchases'
}
