import { LogOut } from 'lucide-react'
import { Link, useLocation } from 'react-router-dom'

import { RequireRole } from '@/components/auth/RequireRole'
import { Avatar } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { ThemeToggle } from '@/components/ui/theme-toggle'
import { useAuth } from '@/context/AuthContext'
import { cn } from '@/lib/utils'

function initialsFor(name: string): string {
  return name
    .split(' ')
    .map((word) => word[0])
    .join('')
    .slice(0, 2)
    .toUpperCase()
}

const navLinkClass = (active: boolean) =>
  cn(
    'rounded-lg px-3.5 py-1.5 text-[13.5px]',
    active ? 'bg-accent font-semibold text-primary' : 'font-medium text-foreground/80 hover:bg-secondary/60',
  )

export function Header() {
  const { user, logout } = useAuth()
  const location = useLocation()

  if (!user) return null

  return (
    <header className="sticky top-0 z-40 flex h-[60px] items-center gap-6 border-b border-divider bg-card/85 px-7 backdrop-blur-md">
      <Link to="/" className="flex items-center gap-[11px]">
        <div className="flex size-[30px] items-center justify-center rounded-lg bg-primary text-[15px] font-bold tracking-tight text-primary-foreground">
          V
        </div>
        <span className="flex items-baseline gap-2">
          <span className="text-base font-semibold tracking-tight">VATDesk</span>
          <span className="tabular-nums-mono rounded-md bg-accent px-1.5 py-0.5 text-[10px] font-medium tracking-wide text-primary">
            ÁFA
          </span>
        </span>
      </Link>

      <nav className="ml-2 flex gap-1">
        <Link to="/" className={navLinkClass(location.pathname === '/')}>
          Declarations
        </Link>
        <RequireRole role="Admin">
          <Link to="/declarations/new" className={navLinkClass(location.pathname === '/declarations/new')}>
            New declaration
          </Link>
        </RequireRole>
      </nav>

      <div className="ml-auto flex items-center gap-1">
        <ThemeToggle />
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button
              type="button"
              className="flex items-center gap-2.5 rounded-[10px] border border-transparent py-[5px] pr-2 pl-[5px] hover:bg-secondary/60"
            >
              <Avatar initials={initialsFor(user.name)} />
              <span className="text-left leading-tight">
                <span className="block text-[13px] font-medium">{user.name}</span>
                <span className="block text-[11px] text-muted-foreground capitalize">{user.role}</span>
              </span>
              <span className="ml-0.5 text-[11px] text-subtle-foreground">▾</span>
            </button>
          </DropdownMenuTrigger>
          <DropdownMenuContent>
            <div className="border-b border-divider p-4">
              <div className="flex items-center gap-[11px]">
                <Avatar initials={initialsFor(user.name)} size="md" />
                <div className="min-w-0">
                  <div className="text-sm font-semibold">{user.name}</div>
                  <div className="tabular-nums-mono truncate text-xs text-muted-foreground">{user.email}</div>
                </div>
              </div>
            </div>
            <div className="border-b border-divider px-4 py-3.5">
              <div className="mb-1.5 text-[11px] font-medium tracking-wide text-subtle-foreground uppercase">
                Account role
              </div>
              <div className="flex items-center gap-2">
                <Badge variant={user.role === 'Admin' ? 'default' : 'secondary'}>{user.role}</Badge>
                <span className="text-xs text-muted-foreground">
                  {user.role === 'Admin' ? 'Full access' : 'Read-only'}
                </span>
              </div>
            </div>
            <div className="flex justify-between border-b border-divider px-4 py-3.5 text-[12.5px] text-muted-foreground">
              <span>Organisation</span>
              <span className="font-medium text-foreground">Be–Ker Kft.</span>
            </div>
            <DropdownMenuItem
              onSelect={logout}
              className="flex items-center gap-2 text-destructive focus:bg-destructive-bg focus:text-destructive"
            >
              <LogOut className="size-4" /> Sign out
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </header>
  )
}
