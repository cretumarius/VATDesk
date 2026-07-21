import type { ReactNode } from 'react'

import type { UserRole } from '@/api/types'
import { useAuth } from '@/context/AuthContext'

interface RequireRoleProps {
  role: UserRole | UserRole[]
  children: ReactNode
  fallback?: ReactNode
}

/**
 * Cosmetic role gate — hides UI the current user's role shouldn't see. The API's
 * [Authorize(Roles = ...)] policies are the real enforcement; this component only ever
 * saves a round trip / avoids showing a button that would 403. Never treat a hidden
 * button here as a substitute for the server check.
 */
export function RequireRole({ role, children, fallback = null }: RequireRoleProps) {
  const { user } = useAuth()
  const allowedRoles = Array.isArray(role) ? role : [role]

  if (!user || !allowedRoles.includes(user.role)) {
    return <>{fallback}</>
  }

  return <>{children}</>
}
