import { useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'

import { ApiError, uploadDeclaration } from '@/api/client'
import { Button } from '@/components/ui/button'
import { DropZone } from '@/components/upload/DropZone'
import { FileCard } from '@/components/upload/FileCard'
import { FormatHintTabs } from '@/components/upload/FormatHintTabs'
import { ProcessingScreen } from '@/components/upload/ProcessingScreen'
import { UploadErrorScreen } from '@/components/upload/UploadErrorScreen'
import { useToast } from '@/context/ToastContext'
import { declarationStatusLabel } from '@/lib/declaration-status'

type Phase =
  | { kind: 'select'; file: File | null }
  | { kind: 'processing'; file: File; step: number }
  | { kind: 'error'; fileName: string }
  | { kind: 'forbidden' }

// The design times its (fake, client-only) processing steps at roughly 1000ms and 2100ms
// in, with the whole sequence resolving around 3200ms. The real API is a single
// request/response — there is no server-sent progress for "parsing" vs "validating" vs
// "calculating" — so these steps are purely a presentational sequence timed to feel
// similar, not driven by anything the server reports.
const STEP_DELAYS_MS = [1000, 2100]
const MIN_DISPLAY_MS = 3200

export function UploadPage() {
  const navigate = useNavigate()
  const { addToast } = useToast()
  const [phase, setPhase] = useState<Phase>({ kind: 'select', file: null })
  const timers = useRef<ReturnType<typeof setTimeout>[]>([])

  function clearTimers() {
    timers.current.forEach(clearTimeout)
    timers.current = []
  }

  async function process(file: File) {
    setPhase({ kind: 'processing', file, step: 0 })

    timers.current.push(
      setTimeout(() => setPhase((p) => (p.kind === 'processing' ? { ...p, step: 1 } : p)), STEP_DELAYS_MS[0]),
      setTimeout(() => setPhase((p) => (p.kind === 'processing' ? { ...p, step: 2 } : p)), STEP_DELAYS_MS[1]),
    )
    const minDisplay = new Promise((resolve) => setTimeout(resolve, MIN_DISPLAY_MS))

    try {
      const [dto] = await Promise.all([uploadDeclaration(file), minDisplay])
      clearTimers()
      addToast('success', 'Declaration ready', `${dto.id} — ${declarationStatusLabel(dto.status)}`)
      navigate(`/declarations/${dto.id}`)
    } catch (err) {
      clearTimers()

      if (err instanceof ApiError) {
        if (err.status === 401) {
          // apiFetch already cleared the session and is redirecting to /login.
          return
        }
        if (err.status === 403) {
          setPhase({ kind: 'forbidden' })
          return
        }
        setPhase({ kind: 'error', fileName: file.name })
        return
      }

      // Not an ApiError: fetch itself failed (offline, DNS, server unreachable).
      addToast('error', 'Upload failed', 'Could not reach the server. Check your connection and try again.', {
        label: 'Retry',
        onClick: () => process(file),
      })
      setPhase({ kind: 'select', file })
    }
  }

  if (phase.kind === 'processing') {
    return <ProcessingScreen fileName={phase.file.name} currentStep={phase.step} />
  }

  if (phase.kind === 'error') {
    return (
      <UploadErrorScreen
        fileName={phase.fileName}
        onBackToDashboard={() => navigate('/')}
        onTryAgain={() => setPhase({ kind: 'select', file: null })}
      />
    )
  }

  if (phase.kind === 'forbidden') {
    return (
      <div className="flex min-h-[calc(100vh-60px)] items-center justify-center p-10">
        <div className="w-full max-w-[440px] text-center">
          <h1 className="mb-2.5 text-[22px] font-semibold tracking-tight">Admins only</h1>
          <p className="mb-6 text-[14.5px] leading-relaxed text-muted-foreground">
            Your account no longer has permission to create declarations. If this seems wrong, ask an admin to check
            your role, or sign in again.
          </p>
          <Button type="button" variant="outline" onClick={() => navigate('/')}>
            Back to dashboard
          </Button>
        </div>
      </div>
    )
  }

  return (
    <main className="mx-auto max-w-[820px] px-7 pt-9 pb-20">
      <button
        type="button"
        onClick={() => navigate('/')}
        className="mb-4 text-[13px] text-muted-foreground hover:text-primary"
      >
        ← Back to declarations
      </button>
      <h1 className="mb-1.5 text-2xl font-semibold tracking-tight">New declaration</h1>
      <p className="mb-[26px] text-sm text-muted-foreground">
        Upload an invoice export. We accept <b className="font-semibold text-foreground">.csv</b> and{' '}
        <b className="font-semibold text-foreground">NAV 3.0 .xml</b> up to 5 MB.
      </p>

      {phase.file ? (
        <FileCard
          file={phase.file}
          onRemove={() => setPhase({ kind: 'select', file: null })}
          onProcess={() => process(phase.file!)}
        />
      ) : (
        <DropZone onFileSelected={(file) => setPhase({ kind: 'select', file })} />
      )}

      <FormatHintTabs />
    </main>
  )
}
