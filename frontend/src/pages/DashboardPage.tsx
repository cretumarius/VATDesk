import { Plus } from 'lucide-react'
import { useNavigate } from 'react-router-dom'

import { RequireRole } from '@/components/auth/RequireRole'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { useAuth } from '@/context/AuthContext'

// Declaration history/listing is Phase 4.4 — this still always shows the empty state,
// even after a real upload exists, until that session wires GET /api/declarations here.
export function DashboardPage() {
  const { user } = useAuth()
  const navigate = useNavigate()

  return (
    <main className="mx-auto max-w-[1120px] px-7 pt-9 pb-20">
      <div className="mb-[26px] flex items-end justify-between gap-5">
        <div>
          <h1 className="mb-[5px] text-2xl font-semibold tracking-tight">Declarations</h1>
          <p className="text-sm text-muted-foreground">No declarations on record</p>
        </div>
        <RequireRole role="Admin">
          <Button type="button" onClick={() => navigate('/declarations/new')}>
            <Plus className="size-4" /> New declaration
          </Button>
        </RequireRole>
      </div>

      <Card className="rounded-2xl px-10 py-14 text-center">
        <CardContent className="flex flex-col items-center p-0">
          <div className="relative mb-[22px] flex h-[88px] w-[120px] items-center justify-center rounded-xl border border-border bg-[repeating-linear-gradient(135deg,var(--color-muted)_0_10px,var(--color-secondary)_10px_20px)]">
            <div className="h-[60px] w-[52px] rounded-md border border-input bg-card shadow-[0_4px_10px_rgba(16,32,30,0.08)]" />
            <div className="absolute right-[22px] bottom-3.5 flex size-[26px] items-center justify-center rounded-full bg-primary text-base font-bold text-primary-foreground">
              +
            </div>
          </div>
          <h2 className="mb-2 text-[19px] font-semibold tracking-tight">No declarations yet</h2>
          <p className="mx-auto mb-[22px] max-w-[380px] text-sm leading-relaxed text-muted-foreground">
            Upload a CSV or NAV 3.0 XML invoice export to generate your first Hungarian VAT declaration summary.
          </p>
          {user?.role === 'Admin' ? (
            <Button type="button" onClick={() => navigate('/declarations/new')}>
              Create your first declaration
            </Button>
          ) : (
            <div className="text-[13px] text-subtle-foreground">
              Your role can view and export declarations. Ask an admin to create one.
            </div>
          )}
        </CardContent>
      </Card>
    </main>
  )
}
