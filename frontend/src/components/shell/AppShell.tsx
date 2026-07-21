import { Outlet } from 'react-router-dom'

import { Header } from './Header'

/** Reusable frame every authenticated page mounts into (header + nav + content outlet). */
export function AppShell() {
  return (
    <div className="min-h-screen bg-background">
      <Header />
      <Outlet />
    </div>
  )
}
