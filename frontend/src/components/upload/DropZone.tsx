import { useRef, useState, type ChangeEvent, type DragEvent } from 'react'
import { Upload as UploadIcon } from 'lucide-react'

import { fetchSampleAsFile } from '@/api/client'
import { Alert } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

const MAX_FILE_SIZE_BYTES = 5 * 1024 * 1024
const ALLOWED_EXTENSIONS = ['.csv', '.xml']

interface DropZoneProps {
  onFileSelected: (file: File) => void
}

/**
 * Client-side checks here are UX only (fast inline feedback, no wasted request) — the
 * server re-validates everything regardless (skill hard rule). Never assume a file that
 * passes these checks is guaranteed to upload successfully.
 */
export function DropZone({ onFileSelected }: DropZoneProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [dragging, setDragging] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [loadingSample, setLoadingSample] = useState<string | null>(null)

  function validateAndSelect(file: File) {
    const name = file.name.toLowerCase()
    const hasAllowedExtension = ALLOWED_EXTENSIONS.some((ext) => name.endsWith(ext))

    if (!hasAllowedExtension) {
      setError('Unsupported file type. Upload a .csv or NAV 3.0 .xml file.')
      return
    }
    if (file.size > MAX_FILE_SIZE_BYTES) {
      setError('That file is larger than 5 MB. Split the export and try again.')
      return
    }
    if (file.size === 0) {
      setError('This file is empty — it contains no invoice rows.')
      return
    }

    setError(null)
    onFileSelected(file)
  }

  function handleDrop(event: DragEvent<HTMLDivElement>) {
    event.preventDefault()
    setDragging(false)
    const file = event.dataTransfer.files?.[0]
    if (file) validateAndSelect(file)
  }

  function handlePick(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    if (file) validateAndSelect(file)
    event.target.value = ''
  }

  async function useSample(kind: 'clean.csv' | 'nav.xml' | 'invalid.csv') {
    setLoadingSample(kind)
    try {
      const file = await fetchSampleAsFile(kind)
      validateAndSelect(file)
    } catch {
      setError('Could not load the sample file. Try again.')
    } finally {
      setLoadingSample(null)
    }
  }

  return (
    <div>
      <div
        role="button"
        tabIndex={0}
        onClick={() => inputRef.current?.click()}
        onKeyDown={(event) => {
          if (event.key === 'Enter' || event.key === ' ') inputRef.current?.click()
        }}
        onDragOver={(event) => {
          event.preventDefault()
          setDragging(true)
        }}
        onDragLeave={(event) => {
          event.preventDefault()
          setDragging(false)
        }}
        onDrop={handleDrop}
        className={cn(
          'flex cursor-pointer flex-col items-center rounded-2xl border-2 border-dashed p-11 text-center transition-colors',
          dragging ? 'border-primary bg-accent/40' : 'border-[#cdd7d5] bg-muted',
        )}
      >
        <input ref={inputRef} type="file" accept=".csv,.xml" className="hidden" onChange={handlePick} />
        <div className="mb-4 flex size-14 items-center justify-center rounded-2xl bg-accent text-primary">
          <UploadIcon className="size-6" />
        </div>
        <div className="mb-1 text-base font-semibold">Drag &amp; drop your file here</div>
        <div className="mb-4 text-[13.5px] text-muted-foreground">
          or <span className="font-semibold text-primary">browse to upload</span> · CSV or NAV 3.0 XML · max 5 MB
        </div>
        <div className="flex flex-wrap justify-center gap-2.5" onClick={(event) => event.stopPropagation()}>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={loadingSample !== null}
            onClick={() => useSample('clean.csv')}
          >
            {loadingSample === 'clean.csv' ? 'Loading…' : 'Use sample .csv'}
          </Button>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={loadingSample !== null}
            onClick={() => useSample('nav.xml')}
          >
            {loadingSample === 'nav.xml' ? 'Loading…' : 'Use sample .xml'}
          </Button>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={loadingSample !== null}
            onClick={() => useSample('invalid.csv')}
          >
            {loadingSample === 'invalid.csv' ? 'Loading…' : 'Use sample with warnings'}
          </Button>
        </div>
      </div>

      {error && (
        <Alert className="mt-3.5">
          <span className="font-bold">!</span>
          <span>{error}</span>
        </Alert>
      )}
    </div>
  )
}
