import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'

import { ToastViewport } from '@/components/ui/toast'

export type ToastType = 'success' | 'error' | 'info'

export interface ToastAction {
  label: string
  onClick: () => void
}

export interface ToastItem {
  id: string
  type: ToastType
  title: string
  message?: string
  action?: ToastAction
}

interface ToastContextValue {
  addToast: (type: ToastType, title: string, message?: string, action?: ToastAction) => void
}

const ToastContext = createContext<ToastContextValue | undefined>(undefined)

const AUTO_DISMISS_MS = 4200

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([])

  const dismissToast = useCallback((id: string) => {
    setToasts((current) => current.filter((toast) => toast.id !== id))
  }, [])

  const addToast = useCallback(
    (type: ToastType, title: string, message?: string, action?: ToastAction) => {
      const id = crypto.randomUUID()
      setToasts((current) => [...current, { id, type, title, message, action }])
      setTimeout(() => dismissToast(id), AUTO_DISMISS_MS)
    },
    [dismissToast],
  )

  const value = useMemo<ToastContextValue>(() => ({ addToast }), [addToast])

  return (
    <ToastContext.Provider value={value}>
      {children}
      <ToastViewport toasts={toasts} onDismiss={dismissToast} />
    </ToastContext.Provider>
  )
}

export function useToast(): ToastContextValue {
  const context = useContext(ToastContext)
  if (!context) {
    throw new Error('useToast must be used within a ToastProvider')
  }
  return context
}
