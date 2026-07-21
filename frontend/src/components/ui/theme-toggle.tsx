import { useEffect, useState } from 'react'
import { Moon, Sun } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { applyTheme, getStoredTheme, type Theme } from '@/lib/theme'

/** Light/dark toggle. Not part of the design (light-mode only) — see session "Design gaps". */
export function ThemeToggle() {
  const [theme, setTheme] = useState<Theme>(() => getStoredTheme())

  useEffect(() => {
    applyTheme(theme)
  }, [theme])

  return (
    <Button
      type="button"
      variant="ghost"
      size="icon"
      aria-label={theme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'}
      onClick={() => setTheme((current) => (current === 'dark' ? 'light' : 'dark'))}
    >
      {theme === 'dark' ? <Sun /> : <Moon />}
    </Button>
  )
}
