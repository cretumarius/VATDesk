import { clearSession, getToken } from '@/lib/auth-storage'
import type { HealthResponse, LoginRequestDto, LoginResponseDto, MeDto } from './types'

interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
}

export class ApiError extends Error {
  status: number
  detail?: string

  constructor(status: number, problem?: ProblemDetails) {
    super(problem?.title ?? `Request failed (${status})`)
    this.status = status
    this.detail = problem?.detail
  }
}

async function parseProblem(response: Response): Promise<ProblemDetails | undefined> {
  try {
    return (await response.json()) as ProblemDetails
  } catch {
    return undefined
  }
}

/**
 * For authenticated endpoints. Attaches the bearer token and, on 401 (token missing,
 * expired, or otherwise rejected), clears the session and hard-redirects to /login with
 * the current location preserved as ?redirect= — the guarded route picks that up after
 * a successful login. Never used for the login call itself (see loginRequest below): a
 * bad-credentials 401 there is a normal inline form error, not a "session died" event.
 */
async function apiFetch<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getToken()
  const headers = new Headers(options.headers)
  headers.set('Accept', 'application/json')
  if (token) headers.set('Authorization', `Bearer ${token}`)

  const response = await fetch(path, { ...options, headers })

  if (response.status === 401) {
    clearSession()
    if (window.location.pathname !== '/login') {
      const redirect = encodeURIComponent(window.location.pathname + window.location.search)
      window.location.assign(`/login?redirect=${redirect}`)
    }
    throw new ApiError(401, await parseProblem(response))
  }

  if (!response.ok) {
    throw new ApiError(response.status, await parseProblem(response))
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export function getHealth(): Promise<HealthResponse> {
  return apiFetch<HealthResponse>('/api/health')
}

export async function loginRequest(credentials: LoginRequestDto): Promise<LoginResponseDto> {
  const response = await fetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify(credentials),
  })

  if (!response.ok) {
    throw new ApiError(response.status, await parseProblem(response))
  }

  return (await response.json()) as LoginResponseDto
}

export function getMe(): Promise<MeDto> {
  return apiFetch<MeDto>('/api/auth/me')
}
