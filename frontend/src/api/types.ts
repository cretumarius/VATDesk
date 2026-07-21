// Mirrors VatDesk.Api.Dtos 1:1 — same names, camelCase. Update alongside the API DTOs.

export interface HealthResponse {
  version: string
  databaseConnected: boolean
}

export type UserRole = 'Admin' | 'Viewer'

export interface LoginRequestDto {
  email: string
  password: string
}

export interface LoginResponseDto {
  token: string
  expiresAt: string
  userId: string
  email: string
  name: string
  role: UserRole
}

export interface MeDto {
  userId: string
  email: string
  name: string
  role: UserRole
}

export type VatKind = 'Percentage' | 'ZeroRated' | 'Exempt' | 'ReverseCharge'

export interface VatCategoryDto {
  code: string
  kind: VatKind
  rate: number | null
  displayNameHu: string
  displayNameEn: string
  sortOrder: number
}

export type Direction = 'Out' | 'In'
export type Severity = 'Info' | 'Warning' | 'Error'
export type SourceFormat = 'Csv' | 'NavXml'
export type DeclarationStatus = 'Completed' | 'CompletedWithWarnings' | 'Failed'

export interface CategoryTotalDto {
  vatCode: string
  direction: Direction
  rowCount: number
  totalNet: number
  totalVat: number
  totalGross: number
}

export interface ValidationIssueDto {
  rowNumber: number
  ruleId: string
  severity: Severity
  message: string
}

export interface ValidationSummaryDto {
  validRows: number
  warningRows: number
  errorRows: number
  issues: ValidationIssueDto[]
}

/** Full declaration shape returned by POST /api/declarations and GET /api/declarations/{id}. */
export interface DeclarationDto {
  id: string
  sourceFilename: string
  sourceFormat: SourceFormat
  countryCode: string
  status: DeclarationStatus
  createdAt: string
  perCategory: CategoryTotalDto[]
  totalOutputVat: number
  totalDeductibleInputVat: number
  netVatPayable: number
  validation: ValidationSummaryDto
}

/** Lightweight shape for GET /api/declarations (history list). */
export interface DeclarationListItemDto {
  id: string
  sourceFilename: string
  sourceFormat: SourceFormat
  countryCode: string
  status: DeclarationStatus
  totalOutputVat: number
  totalDeductibleInputVat: number
  netVatPayable: number
  validRows: number
  warningRows: number
  errorRows: number
  createdAt: string
}
