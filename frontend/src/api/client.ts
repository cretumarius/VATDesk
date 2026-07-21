import axios, { type InternalAxiosRequestConfig } from 'axios'

import { clearSession, getToken } from '@/lib/auth-storage'
import type {
  DeclarationDto,
  DeclarationListItemDto,
  HealthResponse,
  LoginRequestDto,
  LoginResponseDto,
  MeDto,
  VatCategoryDto,
} from './types'

export type SampleFileName = 'clean.csv' | 'invalid.csv' | 'nav.xml'

interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
}

/** Normalized shape every rejected request settles into — network/timeout failures included (status 0), never a raw AxiosError. */
export class ApiError extends Error {
  readonly status: number
  readonly title: string
  readonly detail?: string
  readonly problemDetails?: ProblemDetails

  constructor(status: number, problemDetails?: ProblemDetails) {
    const title = problemDetails?.title ?? (status === 0 ? 'Network error' : `Request failed (${status})`)
    super(title)
    this.name = 'ApiError'
    this.status = status
    this.title = title
    this.detail = problemDetails?.detail
    this.problemDetails = problemDetails
  }
}

const LOGIN_PATH = '/api/auth/login'

/**
 * Single axios instance for the whole app — no component may import axios directly (only
 * this module does). baseURL is intentionally empty: every call site already used
 * relative /api/... paths, proxied by Vite in dev and same-origin in prod: preserving
 * that instead of introducing a new configured base.
 */
export const apiClient = axios.create({
  baseURL: '',
  timeout: 30_000,
  headers: { Accept: 'application/json' },
})

apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = getToken()
  if (token) {
    config.headers.set('Authorization', `Bearer ${token}`)
  }
  return config
})

function isProblemDetailsShape(value: unknown): value is ProblemDetails {
  return typeof value === 'object' && value !== null
}

/**
 * Response bodies come back pre-parsed according to the request's responseType — for the
 * blob-fetching calls (samples, PDF) that means a ProblemDetails error body from the
 * server also arrives as a Blob, not JSON, and has to be read out explicitly.
 */
async function extractProblemDetails(data: unknown): Promise<ProblemDetails | undefined> {
  if (data instanceof Blob) {
    try {
      return JSON.parse(await data.text()) as ProblemDetails
    } catch {
      return undefined
    }
  }
  return isProblemDetailsShape(data) ? data : undefined
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: unknown) => {
    if (!axios.isAxiosError(error) || !error.response) {
      // Network failure, timeout, or anything else with no server response at all.
      return Promise.reject(new ApiError(0))
    }

    const { status, data } = error.response
    const problemDetails = await extractProblemDetails(data)

    // A 401 from the login call itself is "wrong password" — a normal inline form error,
    // never a "session died" event, so it must NOT trigger the redirect below.
    const isLoginRequest = error.config?.url === LOGIN_PATH

    if (status === 401 && !isLoginRequest) {
      clearSession()
      if (window.location.pathname !== '/login') {
        const redirect = encodeURIComponent(window.location.pathname + window.location.search)
        window.location.assign(`/login?redirect=${redirect}`)
      }
    }

    return Promise.reject(new ApiError(status, problemDetails))
  },
)

export function getHealth(): Promise<HealthResponse> {
  return apiClient.get<HealthResponse>('/api/health').then((r) => r.data)
}

export function loginRequest(credentials: LoginRequestDto): Promise<LoginResponseDto> {
  return apiClient.post<LoginResponseDto>(LOGIN_PATH, credentials).then((r) => r.data)
}

export function getMe(): Promise<MeDto> {
  return apiClient.get<MeDto>('/api/auth/me').then((r) => r.data)
}

export function getDeclarations(): Promise<DeclarationListItemDto[]> {
  return apiClient.get<DeclarationListItemDto[]>('/api/declarations').then((r) => r.data)
}

export function getDeclaration(id: string): Promise<DeclarationDto> {
  return apiClient.get<DeclarationDto>(`/api/declarations/${id}`).then((r) => r.data)
}

/**
 * Multipart upload — a single request/response. Passing FormData as the body and never
 * setting Content-Type ourselves is what lets axios (via the browser) fill in
 * multipart/form-data with its own boundary; hard-coding it here would break the upload.
 */
export function uploadDeclaration(file: File, signal?: AbortSignal): Promise<DeclarationDto> {
  const formData = new FormData()
  formData.append('file', file)
  return apiClient.post<DeclarationDto>('/api/declarations', formData, { signal }).then((r) => r.data)
}

function fetchSampleBlob(name: SampleFileName): Promise<Blob> {
  return apiClient.get<Blob>(`/api/samples/${name}`, { responseType: 'blob' }).then((r) => r.data)
}

/** Fetches a sample and returns it as a File, for the upload page's "use sample" buttons. */
export async function fetchSampleAsFile(name: SampleFileName): Promise<File> {
  const blob = await fetchSampleBlob(name)
  const type = name.endsWith('.xml') ? 'application/xml' : 'text/csv'
  return new File([blob], `sample-${name}`, { type })
}

function saveBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob)
  try {
    const anchor = document.createElement('a')
    anchor.href = url
    anchor.download = filename
    document.body.appendChild(anchor)
    anchor.click()
    anchor.remove()
  } finally {
    URL.revokeObjectURL(url)
  }
}

/** Fetches a sample and saves it to disk — plain <a href> can't carry the auth header. */
export async function downloadSample(name: SampleFileName): Promise<void> {
  const blob = await fetchSampleBlob(name)
  saveBlob(blob, `sample-${name}`)
}

/** Streams the declaration PDF and saves it to disk. Available to both roles. */
export async function downloadDeclarationPdf(id: string): Promise<void> {
  const blob = await apiClient
    .get<Blob>(`/api/declarations/${id}/pdf`, { responseType: 'blob' })
    .then((r) => r.data)
  saveBlob(blob, `declaration-${id.slice(0, 8)}.pdf`)
}

const vatCategoriesCache = new Map<string, Promise<VatCategoryDto[]>>()

/** Registry is fixed per country for the lifetime of the tab — cached so every report/upload view doesn't refetch it. */
export function getVatCategories(countryCode: string): Promise<VatCategoryDto[]> {
  let cached = vatCategoriesCache.get(countryCode)
  if (!cached) {
    cached = apiClient.get<VatCategoryDto[]>(`/api/countries/${countryCode}/vat-categories`).then((r) => r.data)
    cached.catch(() => vatCategoriesCache.delete(countryCode))
    vatCategoriesCache.set(countryCode, cached)
  }
  return cached
}
