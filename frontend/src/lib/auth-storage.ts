import type { UserRole } from '@/api/types'

const TOKEN_KEY = 'vatdesk-token'
const USER_KEY = 'vatdesk-user'

export interface StoredUser {
  userId: string
  email: string
  name: string
  role: UserRole
}

// Token lives primarily in memory (AuthContext React state); sessionStorage here is only
// a rehydration mechanism so a page refresh doesn't force a re-login. Tradeoff: anything
// reachable via sessionStorage is readable by an XSS payload, same as localStorage — an
// httpOnly cookie would be the safer pattern, but that needs server-issued cookies/CSRF
// handling this demo's scope doesn't build. sessionStorage (vs. localStorage) at least
// scopes the exposure to the tab's lifetime rather than persisting indefinitely.
export function getToken(): string | null {
  return sessionStorage.getItem(TOKEN_KEY)
}

export function getStoredUser(): StoredUser | null {
  const raw = sessionStorage.getItem(USER_KEY)
  if (!raw) return null
  try {
    return JSON.parse(raw) as StoredUser
  } catch {
    return null
  }
}

export function setSession(token: string, user: StoredUser): void {
  sessionStorage.setItem(TOKEN_KEY, token)
  sessionStorage.setItem(USER_KEY, JSON.stringify(user))
}

export function clearSession(): void {
  sessionStorage.removeItem(TOKEN_KEY)
  sessionStorage.removeItem(USER_KEY)
}
