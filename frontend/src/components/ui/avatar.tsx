import * as React from 'react'
import { cva, type VariantProps } from 'class-variance-authority'

import { cn } from '@/lib/utils'

const avatarVariants = cva(
  'inline-flex shrink-0 items-center justify-center rounded-full bg-primary font-semibold text-primary-foreground',
  {
    variants: {
      size: {
        sm: 'size-8 text-[13px]',
        md: 'size-10 text-[15px]',
      },
    },
    defaultVariants: { size: 'sm' },
  },
)

interface AvatarProps extends React.ComponentProps<'div'>, VariantProps<typeof avatarVariants> {
  initials: string
}

function Avatar({ className, size, initials, ...props }: AvatarProps) {
  return (
    <div data-slot="avatar" className={cn(avatarVariants({ size, className }))} {...props}>
      {initials}
    </div>
  )
}

export { Avatar }
