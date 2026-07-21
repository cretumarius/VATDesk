import { useState } from 'react'

import { downloadSample } from '@/api/client'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'

interface UploadErrorScreenProps {
  fileName: string
  onTryAgain: () => void
  onBackToDashboard: () => void
}

const CHECKLIST = [
  "The file is a valid CSV or NAV 3.0 XML (not a spreadsheet or PDF export).",
  "Column headers match the expected structure and aren't renamed.",
  'The export is not empty and uses UTF-8 encoding.',
]

export function UploadErrorScreen({ fileName, onTryAgain, onBackToDashboard }: UploadErrorScreenProps) {
  const [downloading, setDownloading] = useState<string | null>(null)

  async function handleDownload(name: 'clean.csv' | 'nav.xml') {
    setDownloading(name)
    try {
      await downloadSample(name)
    } finally {
      setDownloading(null)
    }
  }

  return (
    <div className="flex min-h-[calc(100vh-60px)] items-center justify-center p-10">
      <div className="w-full max-w-[500px] text-center">
        <div className="mx-auto mb-[22px] flex size-[66px] items-center justify-center rounded-full border border-destructive-border bg-destructive-bg text-[32px] font-bold text-destructive">
          !
        </div>
        <h1 className="mb-2.5 text-[22px] font-semibold tracking-tight">We couldn't process that file</h1>
        <p className="mx-auto mb-6 max-w-[410px] text-[14.5px] leading-relaxed text-muted-foreground">
          The file <span className="tabular-nums-mono text-foreground">{fileName}</span> couldn't be parsed. It may
          be malformed, use an unexpected schema, or contain no invoice rows.
        </p>

        <Card className="mb-6 p-5 text-left">
          <div className="mb-3 text-xs font-semibold tracking-wide text-table-header-foreground uppercase">
            What to check
          </div>
          <div className="flex flex-col gap-2.5">
            {CHECKLIST.map((item) => (
              <div key={item} className="flex gap-2.5 text-[13.5px] leading-relaxed text-foreground/85">
                <span className="font-bold text-primary">•</span>
                {item}
              </div>
            ))}
          </div>
          <div className="mt-4 flex gap-[18px] border-t border-divider pt-3.5">
            <button
              type="button"
              onClick={() => handleDownload('clean.csv')}
              disabled={downloading !== null}
              className="text-sm text-primary hover:underline disabled:opacity-50"
            >
              ↓ Sample .csv
            </button>
            <button
              type="button"
              onClick={() => handleDownload('nav.xml')}
              disabled={downloading !== null}
              className="text-sm text-primary hover:underline disabled:opacity-50"
            >
              ↓ Sample NAV 3.0 .xml
            </button>
          </div>
        </Card>

        <div className="flex justify-center gap-2.5">
          <Button type="button" variant="outline" onClick={onBackToDashboard}>
            Back to dashboard
          </Button>
          <Button type="button" onClick={onTryAgain}>
            Try again
          </Button>
        </div>
      </div>
    </div>
  )
}
