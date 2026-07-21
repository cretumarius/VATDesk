// Mirrors VatDesk.Api.Dtos 1:1 — same names, camelCase. Update alongside the API DTOs.

export interface HealthResponse {
  version: string
  databaseConnected: boolean
}
