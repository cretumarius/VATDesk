import { X } from 'lucide-react'

import { cn } from '@/lib/utils'
import type { ToastItem } from '@/context/ToastContext'

const TYPE_STYLES: Record<ToastItem['type'], { border: string; iconBg: string; iconFg: string; icon: string }> = {
  success: { border: 'border-l-success', iconBg: 'bg-success-bg', iconFg: 'text-success', icon: '✓' },
  error: { border: 'border-l-destructive', iconBg: 'bg-destructive-bg', iconFg: 'text-destructive', icon: '!' },
  info: { border: 'border-l-primary', iconBg: 'bg-accent', iconFg: 'text-primary', icon: 'i' },
}

interface ToastViewportProps {
  toasts: ToastItem[]
  onDismiss: (id: string) => void
}

export function ToastViewport({ toasts, onDismiss }: ToastViewportProps) {
  if (toasts.length === 0) return null

  return (
    <div className="fixed right-[22px] bottom-[22px] z-[60] flex max-w-[340px] flex-col gap-2.5">
      {toasts.map((toast) => {
        const style = TYPE_STYLES[toast.type]
        return (
          <div
            key={toast.id}
            role="status"
            className={cn(
              'flex items-start gap-3 rounded-[11px] border border-border border-l-4 bg-card p-[13px_15px] shadow-[0_12px_30px_-10px_rgba(16,32,30,0.28)]',
              style.border,
            )}
          >
            <div
              className={cn(
                'flex size-[22px] shrink-0 items-center justify-center rounded-full text-xs font-bold',
                style.iconBg,
                style.iconFg,
              )}
            >
              {style.icon}
            </div>
            <div className="flex-1">
              <div className="text-[13.5px] font-semibold">{toast.title}</div>
              {toast.message && <div className="mt-0.5 text-[12.5px] text-muted-foreground">{toast.message}</div>}
              {toast.action && (
                <button
                  type="button"
                  onClick={() => {
                    toast.action?.onClick()
                    onDismiss(toast.id)
                  }}
                  className="mt-1.5 text-[12.5px] font-semibold text-primary hover:underline"
                >
                  {toast.action.label}
                </button>
              )}
            </div>
            <button
              type="button"
              onClick={() => onDismiss(toast.id)}
              aria-label="Dismiss"
              className="text-subtle-foreground hover:text-foreground"
            >
              <X className="size-4" />
            </button>
          </div>
        )
      })}
    </div>
  )
}
