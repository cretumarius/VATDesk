import { clearSession, getToken } from '@/lib/auth-storage'
import type { DeclarationDto, HealthResponse, LoginRequestDto, LoginResponseDto, MeDto } from './types'

export type SampleFileName = 'clean.csv' | 'invalid.csv' | 'nav.xml'

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

export function getDeclaration(id: string): Promise<DeclarationDto> {
  return apiFetch<DeclarationDto>(`/api/declarations/${id}`)
}

/**
 * Multipart upload — a single request/response. apiFetch already omits Content-Type
 * when none is passed, which is required here: the browser must set
 * multipart/form-data with its own boundary, not us.
 */
export function uploadDeclaration(file: File, signal?: AbortSignal): Promise<DeclarationDto> {
  const formData = new FormData()
  formData.append('file', file)
  return apiFetch<DeclarationDto>('/api/declarations', { method: 'POST', body: formData, signal })
}

async function fetchSampleBlob(name: SampleFileName): Promise<Blob> {
  const token = getToken()
  const headers = new Headers()
  if (token) headers.set('Authorization', `Bearer ${token}`)

  const response = await fetch(`/api/samples/${name}`, { headers })

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

  return response.blob()
}

/** Fetches a sample and returns it as a File, for the upload page's "use sample" buttons. */
export async function fetchSampleAsFile(name: SampleFileName): Promise<File> {
  const blob = await fetchSampleBlob(name)
  const type = name.endsWith('.xml') ? 'application/xml' : 'text/csv'
  return new File([blob], `sample-${name}`, { type })
}

/** Fetches a sample and saves it to disk — plain <a href> can't carry the auth header. */
export async function downloadSample(name: SampleFileName): Promise<void> {
  const blob = await fetchSampleBlob(name)
  const url = URL.createObjectURL(blob)
  try {
    const anchor = document.createElement('a')
    anchor.href = url
    anchor.download = `sample-${name}`
    document.body.appendChild(anchor)
    anchor.click()
    anchor.remove()
  } finally {
    URL.revokeObjectURL(url)
  }
}
