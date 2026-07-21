import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'

import { ApiError, getDeclaration } from '@/api/client'
import type { DeclarationDto } from '@/api/types'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { declarationStatusBadgeVariant, declarationStatusLabel } from '@/lib/declaration-status'

type State =
  | { kind: 'loading' }
  | { kind: 'loaded'; declaration: DeclarationDto }
  | { kind: 'not-found' }
  | { kind: 'error' }

/**
 * Phase 4.2 scope boundary: full report view (summary cards, category table, validation
 * panel) is Phase 4.3. This route only proves the navigation target exists and shows
 * enough to confirm the upload actually completed.
 */
export function DeclarationStubPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [state, setState] = useState<State>({ kind: 'loading' })

  useEffect(() => {
    if (!id) return undefined

    let cancelled = false
    setState({ kind: 'loading' })

    getDeclaration(id)
      .then((declaration) => {
        if (!cancelled) setState({ kind: 'loaded', declaration })
      })
      .catch((err: unknown) => {
        if (cancelled) return
        setState({ kind: err instanceof ApiError && err.status === 404 ? 'not-found' : 'error' })
      })

    return () => {
      cancelled = true
    }
  }, [id])

  if (state.kind === 'loading') {
    return (
      <main className="mx-auto max-w-[820px] px-7 pt-9 pb-20">
        <p className="text-sm text-muted-foreground">Loading declaration…</p>
      </main>
    )
  }

  if (state.kind === 'not-found' || state.kind === 'error') {
    return (
      <main className="mx-auto max-w-[820px] px-7 pt-9 pb-20 text-center">
        <h1 className="mb-2 text-xl font-semibold">
          {state.kind === 'not-found' ? 'Declaration not found' : 'Something went wrong'}
        </h1>
        <p className="mb-6 text-sm text-muted-foreground">
          {state.kind === 'not-found'
            ? "This declaration doesn't exist, or you don't have access to it."
            : 'Could not load this declaration. Please try again.'}
        </p>
        <Button type="button" variant="outline" onClick={() => navigate('/')}>
          Back to dashboard
        </Button>
      </main>
    )
  }

  const { declaration } = state

  return (
    <main className="mx-auto max-w-[820px] px-7 pt-9 pb-20">
      <button
        type="button"
        onClick={() => navigate('/')}
        className="mb-4 text-[13px] text-muted-foreground hover:text-primary"
      >
        ← Back to dashboard
      </button>

      <div className="mb-2 flex flex-wrap items-center gap-3">
        <h1 className="tabular-nums-mono text-[22px] font-semibold tracking-tight">{declaration.id}</h1>
        <Badge variant={declarationStatusBadgeVariant(declaration.status)}>
          {declarationStatusLabel(declaration.status)}
        </Badge>
      </div>
      <p className="mb-8 text-sm text-muted-foreground">
        Source · <span className="tabular-nums-mono text-foreground">{declaration.sourceFilename}</span>
      </p>

      <Card className="p-14 text-center">
        <CardContent className="p-0">
          <p className="text-sm text-muted-foreground">Full report view coming soon.</p>
        </CardContent>
      </Card>
    </main>
  )
}
