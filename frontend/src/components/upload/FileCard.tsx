import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'

interface FileCardProps {
  file: File
  onRemove: () => void
  onProcess: () => void
}

function extensionLabel(fileName: string): string {
  const dot = fileName.lastIndexOf('.')
  return dot === -1 ? '' : fileName.slice(dot + 1).toUpperCase()
}

function formatSize(bytes: number): string {
  return `${(bytes / 1024).toFixed(0)} KB`
}

export function FileCard({ file, onRemove, onProcess }: FileCardProps) {
  const ext = extensionLabel(file.name)

  return (
    <Card className="flex items-center gap-4 p-[18px_20px]">
      <div className="flex size-[46px] shrink-0 items-center justify-center rounded-[10px] bg-accent text-xs font-semibold text-primary uppercase">
        {ext}
      </div>
      <div className="min-w-0 flex-1">
        <div className="tabular-nums-mono truncate text-sm font-medium">{file.name}</div>
        <div className="mt-0.5 text-[12.5px] text-muted-foreground">
          {formatSize(file.size)} · {ext} · ready to process
        </div>
      </div>
      <Button type="button" variant="ghost" onClick={onRemove}>
        Remove
      </Button>
      <Button type="button" onClick={onProcess}>
        Process
      </Button>
    </Card>
  )
}
