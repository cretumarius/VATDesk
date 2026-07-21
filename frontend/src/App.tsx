import { Navigate, Route, BrowserRouter as Router, Routes } from 'react-router-dom'

import { RequireAuth } from '@/components/auth/RequireAuth'
import { AppShell } from '@/components/shell/AppShell'
import { AuthProvider } from '@/context/AuthContext'
import { DashboardPage } from '@/pages/DashboardPage'
import { LoginPage } from '@/pages/LoginPage'

function App() {
  return (
    <Router>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route element={<RequireAuth />}>
            <Route element={<AppShell />}>
              <Route path="/" element={<DashboardPage />} />
            </Route>
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AuthProvider>
    </Router>
  )
}

export default App
