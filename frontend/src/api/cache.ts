/**
 * Cache policy
 * ------------
 * A minimal, explicit, in-memory cache — a plain Map, no library, no TTL. TTL isn't
 * needed because invalidation here is purely event-driven, not time-based:
 *
 * - GET /api/countries/HU/vat-categories: effectively immutable reference data. Cached
 *   for the lifetime of the session, shared across every page that needs it, so
 *   navigating between report views never refetches it.
 * - GET /api/auth/me: cached until logout or a 401. The authenticated user's identity
 *   doesn't change mid-session.
 * - Nothing else goes through this cache: the declarations list (Phase 4.4 owns its own
 *   freshness), individual declaration fetches (cheap, and status could change), and no
 *   non-GET request is ever a candidate.
 *
 * Invalidation is exactly two triggers, both explicit, both full-clear (not per-key):
 * logout (AuthContext.logout) and a 401 response (api/client.ts's response interceptor).
 * A failed request is never cached — see `cached()` below.
 *
 * Not TanStack Query or any other data-fetching library — deliberately out of scope for
 * this session (noted as a possible future improvement instead of adopted here).
 */

const cache = new Map<string, Promise<unknown>>()

/** Returns the cached promise for `key` if one exists, otherwise calls `fetcher`, caches its promise, and returns that. A rejected fetch is evicted immediately so the next call retries instead of replaying the same failure. */
export function cached<T>(key: string, fetcher: () => Promise<T>): Promise<T> {
  const existing = cache.get(key)
  if (existing) {
    return existing as Promise<T>
  }

  const promise = fetcher().catch((error: unknown) => {
    cache.delete(key)
    throw error
  })
  cache.set(key, promise)
  return promise
}

/** Full clear — called on logout and on a 401 redirect. Never partial/per-key. */
export function clearApiCache(): void {
  cache.clear()
}
