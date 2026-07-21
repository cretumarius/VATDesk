import { Card } from '@/components/ui/card'
import { cn } from '@/lib/utils'

const STEP_LABELS = ['Parsing invoice rows', 'Validating against NAV rules', 'Calculating VAT totals']

interface ProcessingScreenProps {
  fileName: string
  /** 0-based index of the step currently in progress. Presentational only — see UploadPage. */
  currentStep: number
}

export function ProcessingScreen({ fileName, currentStep }: ProcessingScreenProps) {
  return (
    <div className="flex min-h-[calc(100vh-60px)] items-center justify-center p-10">
      <Card className="w-full max-w-[440px] p-9 shadow-[0_20px_50px_-20px_rgba(16,32,30,0.22)]">
        <div className="relative mx-auto mb-[22px] size-[58px]">
          <div className="absolute inset-0 rounded-full border-4 border-accent" />
          <div className="absolute inset-0 animate-spin rounded-full border-4 border-primary border-r-transparent border-b-transparent" />
        </div>
        <h2 className="mb-1 text-center text-lg font-semibold">Processing your file</h2>
        <p className="tabular-nums-mono mb-[26px] text-center text-[12.5px] text-muted-foreground">{fileName}</p>

        <div className="flex flex-col gap-1">
          {STEP_LABELS.map((label, index) => {
            const done = index < currentStep
            const active = index === currentStep
            return (
              <div
                key={label}
                className={cn('flex items-center gap-[13px] rounded-[10px] p-[11px_12px]', active && 'bg-muted')}
              >
                <div
                  className={cn(
                    'flex size-[26px] shrink-0 items-center justify-center rounded-full text-[13px] font-semibold',
                    done || active ? 'bg-primary text-primary-foreground' : 'bg-secondary text-subtle-foreground',
                  )}
                >
                  {done ? '✓' : index + 1}
                </div>
                <div className={cn('flex-1 text-sm font-medium', !done && !active && 'text-subtle-foreground')}>
                  {label}
                </div>
                <div
                  className={cn(
                    'text-xs',
                    done && 'text-success',
                    active && 'text-primary',
                    !done && !active && 'text-subtle-foreground',
                  )}
                >
                  {done ? 'Done' : active ? 'Working…' : 'Waiting'}
                </div>
              </div>
            )
          })}
        </div>
      </Card>
    </div>
  )
}
