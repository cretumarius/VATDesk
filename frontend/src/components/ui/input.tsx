import * as React from 'react'

import { cn } from '@/lib/utils'

function Input({ className, type, ...props }: React.ComponentProps<'input'>) {
  return (
    <input
      type={type}
      data-slot="input"
      className={cn(
        'flex h-11 w-full rounded-[10px] border border-input bg-card px-3.5 text-sm text-foreground placeholder:text-subtle-foreground transition-colors outline-none',
        'focus-visible:border-primary focus-visible:ring-4 focus-visible:ring-primary/15',
        'disabled:cursor-not-allowed disabled:opacity-50',
        'aria-invalid:border-destructive aria-invalid:ring-destructive/15',
        className,
      )}
      {...props}
    />
  )
}

export { Input }
