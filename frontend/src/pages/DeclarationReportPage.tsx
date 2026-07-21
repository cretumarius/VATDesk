import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'

import { ApiError, downloadDeclarationPdf, getDeclaration, getVatCategories } from '@/api/client'
import type { DeclarationDto, VatCategoryDto } from '@/api/types'
import { CategoryBreakdownTable } from '@/components/report/CategoryBreakdownTable'
import { ReportHeader } from '@/components/report/ReportHeader'
import { ReportSkeleton } from '@/components/report/ReportSkeleton'
import { SummaryCards } from '@/components/report/SummaryCards'
import { ValidationPanel } from '@/components/report/ValidationPanel'
import { Button } from '@/components/ui/button'
import { useToast } from '@/context/ToastContext'

type State =
  | { kind: 'loading' }
  | { kind: 'loaded'; declaration: DeclarationDto; categories: VatCategoryDto[] }
  | { kind: 'not-found' }
  | { kind: 'error' }

export function DeclarationReportPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { addToast } = useToast()
  const [state, setState] = useState<State>({ kind: 'loading' })
  const [downloadingPdf, setDownloadingPdf] = useState(false)

  useEffect(() => {
    if (!id) return undefined

    let cancelled = false
    setState({ kind: 'loading' })

    Promise.all([getDeclaration(id), getVatCategories('HU')])
      .then(([declaration, categories]) => {
        if (!cancelled) setState({ kind: 'loaded', declaration, categories })
      })
      .catch((err: unknown) => {
        if (cancelled) return
        setState({ kind: err instanceof ApiError && err.status === 404 ? 'not-found' : 'error' })
      })

    return () => {
      cancelled = true
    }
  }, [id])

  async function handleDownloadPdf() {
    if (!id) return
    setDownloadingPdf(true)
    try {
      await downloadDeclarationPdf(id)
    } catch {
      addToast('error', 'PDF download failed', 'Could not generate the PDF. Please try again.')
    } finally {
      setDownloadingPdf(false)
    }
  }

  if (state.kind === 'loading') {
    return (
      <main className="mx-auto max-w-[1120px] px-7 pt-9 pb-20">
        <ReportSkeleton />
      </main>
    )
  }

  if (state.kind === 'not-found' || state.kind === 'error') {
    return (
      <main className="mx-auto max-w-[500px] px-7 pt-24 pb-20 text-center">
        <div className="mx-auto mb-[22px] flex size-[66px] items-center justify-center rounded-full border border-destructive-border bg-destructive-bg text-[32px] font-bold text-destructive">
          !
        </div>
        <h1 className="mb-2.5 text-[22px] font-semibold tracking-tight">
          {state.kind === 'not-found' ? 'Declaration not found' : 'Something went wrong'}
        </h1>
        <p className="mx-auto mb-6 max-w-[410px] text-[14.5px] leading-relaxed text-muted-foreground">
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

  const { declaration, categories } = state

  return (
    <main className="mx-auto max-w-[1120px] px-7 pt-9 pb-20">
      <ReportHeader declaration={declaration} onDownloadPdf={handleDownloadPdf} downloadingPdf={downloadingPdf} />
      <SummaryCards declaration={declaration} />
      <ValidationPanel validation={declaration.validation} />
      <CategoryBreakdownTable perCategory={declaration.perCategory} categories={categories} />
    </main>
  )
}
