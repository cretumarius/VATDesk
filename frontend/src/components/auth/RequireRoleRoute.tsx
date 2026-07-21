import { Navigate, Outlet } from 'react-router-dom'

import type { UserRole } from '@/api/types'
import { useAuth } from '@/context/AuthContext'

interface RequireRoleRouteProps {
  role: UserRole | UserRole[]
}

/**
 * Route-level role bounce (e.g. a Viewer typing /declarations/new directly). Still
 * cosmetic, same as RequireRole — the API's [Authorize(Roles = ...)] policies are the
 * real enforcement; this only avoids showing a page whose actions would all 403.
 */
export function RequireRoleRoute({ role }: RequireRoleRouteProps) {
  const { user } = useAuth()
  const allowedRoles = Array.isArray(role) ? role : [role]

  if (!user || !allowedRoles.includes(user.role)) {
    return <Navigate to="/" replace />
  }

  return <Outlet />
}
