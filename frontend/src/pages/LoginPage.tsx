import { useState, type FormEvent } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'

import { ApiError } from '@/api/client'
import { Alert } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAuth } from '@/context/AuthContext'

// The design's mockup was a client-only fake login where "any password works"; our real
// backend checks the actual password, so these buttons autofill the true demo credentials
// rather than leaving the password field empty (see session "Design gaps").
const DEMO_ADMIN = { email: 'admin@demo.hu', password: 'Admin123!' }
const DEMO_VIEWER = { email: 'viewer@demo.hu', password: 'Viewer123!' }

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const redirectTo = new URLSearchParams(location.search).get('redirect') || '/'

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()

    if (!email.trim() || !password) {
      setError('Enter both your email and password.')
      return
    }

    setError(null)
    setSubmitting(true)
    try {
      await login(email.trim(), password)
      navigate(redirectTo, { replace: true })
    } catch (err) {
      setError(err instanceof ApiError && err.status === 401 ? 'Invalid email or password.' : 'Something went wrong. Please try again.')
    } finally {
      setSubmitting(false)
    }
  }

  function fill(demo: { email: string; password: string }) {
    setEmail(demo.email)
    setPassword(demo.password)
    setError(null)
  }

  return (
    <div className="grid min-h-screen grid-cols-1 lg:grid-cols-[1.05fr_0.95fr]">
      <div className="flex items-center justify-center p-12">
        <div className="w-full max-w-[380px]">
          <div className="mb-[34px] flex items-center gap-[11px]">
            <div className="flex size-9 items-center justify-center rounded-[9px] bg-primary text-lg font-bold text-primary-foreground">
              V
            </div>
            <span className="text-[19px] font-semibold tracking-tight">VATDesk</span>
          </div>

          <h1 className="mb-2 text-[26px] font-semibold tracking-tight">Sign in to your workspace</h1>
          <p className="mb-[30px] text-[14.5px] leading-relaxed text-muted-foreground">
            Generate compliant Hungarian ÁFA declarations from your invoice exports in minutes.
          </p>

          {error && (
            <Alert className="mb-[18px]">
              <span className="font-bold">!</span>
              <span>{error}</span>
            </Alert>
          )}

          <form onSubmit={handleSubmit} noValidate>
            <div className="mb-4">
              <Label htmlFor="email">Email address</Label>
              <Input
                id="email"
                type="email"
                placeholder="you@company.hu"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                disabled={submitting}
                autoComplete="username"
              />
            </div>
            <div className="mb-[22px]">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                placeholder="••••••••"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                disabled={submitting}
                autoComplete="current-password"
              />
            </div>
            <Button type="submit" size="lg" className="w-full" disabled={submitting}>
              {submitting ? 'Signing in…' : 'Sign in'}
            </Button>
          </form>

          <div className="mt-[26px] rounded-xl border border-dashed border-[#cdd7d5] bg-[#f4f8f7] p-4 dark:border-white/15 dark:bg-white/[.04]">
            <div className="mb-[10px] text-[11px] font-semibold tracking-wide text-primary uppercase">
              Demo credentials
            </div>
            <div className="flex flex-col gap-2">
              <button
                type="button"
                onClick={() => fill(DEMO_ADMIN)}
                className="flex items-center justify-between gap-2.5 rounded-[9px] border border-border bg-card px-[11px] py-[9px] text-left hover:border-primary"
              >
                <span className="tabular-nums-mono text-[12.5px]">admin@demo.hu</span>
                <Badge>Admin</Badge>
              </button>
              <button
                type="button"
                onClick={() => fill(DEMO_VIEWER)}
                className="flex items-center justify-between gap-2.5 rounded-[9px] border border-border bg-card px-[11px] py-[9px] text-left hover:border-primary"
              >
                <span className="tabular-nums-mono text-[12.5px]">viewer@demo.hu</span>
                <Badge variant="secondary">Viewer</Badge>
              </button>
            </div>
            <div className="mt-[9px] text-[11.5px] text-subtle-foreground">Click a row to autofill</div>
          </div>
        </div>
      </div>

      <div className="relative hidden items-center justify-center overflow-hidden bg-[linear-gradient(160deg,#0f766e_0%,#0b5d56_55%,#0a4f49_100%)] p-12 lg:flex">
        <div className="absolute inset-0 opacity-[.14] [background-image:repeating-linear-gradient(90deg,#fff_0_1px,transparent_1px_64px),repeating-linear-gradient(0deg,#fff_0_1px,transparent_1px_64px)]" />
        <div className="relative w-full max-w-[400px] text-white">
          <div className="mb-4 text-xs font-medium tracking-wide text-[#8fd3c9] uppercase">
            NAV 3.0 compatible
          </div>
          <div className="text-[26px] leading-[1.32] font-semibold tracking-tight">
            From invoice export to a filing-ready ÁFA summary — reconciled, validated, and audit-traceable.
          </div>
          <div className="mt-8 rounded-[14px] border border-white/15 bg-white/[.09] p-5 backdrop-blur-sm">
            <div className="mb-3 flex items-baseline justify-between">
              <span className="text-[12.5px] text-[#bfe6df]">Net VAT payable · 2025 Q2</span>
              <span className="text-[11px] text-[#8fd3c9]">Completed</span>
            </div>
            <div className="tabular-nums-mono text-[30px] font-semibold tracking-tight">HUF 5,503,000</div>
            <div className="mt-4 flex gap-1.5">
              <div className="h-1.5 flex-1 rounded bg-white/85" />
              <div className="h-1.5 flex-[.55] rounded bg-white/40" />
            </div>
            <div className="mt-2 flex justify-between text-[11.5px] text-[#bfe6df]">
              <span>Output VAT 11.9M</span>
              <span>Input VAT 6.4M</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
