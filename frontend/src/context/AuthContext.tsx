import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'

import { loginRequest } from '@/api/client'
import { clearSession, getStoredUser, getToken, setSession, type StoredUser } from '@/lib/auth-storage'

interface AuthContextValue {
  user: StoredUser | null
  token: string | null
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  // Rehydrated once from sessionStorage on mount (refresh survival); from here on the
  // token/user live in this component's memory for the rest of the session — see
  // lib/auth-storage.ts for the sessionStorage-vs-memory tradeoff this is built on.
  const [token, setToken] = useState<string | null>(() => getToken())
  const [user, setUser] = useState<StoredUser | null>(() => getStoredUser())

  const login = useCallback(async (email: string, password: string) => {
    const response = await loginRequest({ email, password })
    const storedUser: StoredUser = {
      userId: response.userId,
      email: response.email,
      name: response.name,
      role: response.role,
    }
    setSession(response.token, storedUser)
    setToken(response.token)
    setUser(storedUser)
  }, [])

  const logout = useCallback(() => {
    clearSession()
    setToken(null)
    setUser(null)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({ user, token, isAuthenticated: token !== null && user !== null, login, logout }),
    [user, token, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
