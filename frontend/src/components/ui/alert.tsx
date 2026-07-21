import * as React from 'react'

import { cn } from '@/lib/utils'

function Alert({ className, ...props }: React.ComponentProps<'div'>) {
  return (
    <div
      role="alert"
      data-slot="alert"
      className={cn(
        'flex items-start gap-2.5 rounded-[10px] border border-destructive-border bg-destructive-bg px-3.5 py-3 text-[13px] text-destructive-text',
        className,
      )}
      {...props}
    />
  )
}

export { Alert }
