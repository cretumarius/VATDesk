import { Navigate, Route, BrowserRouter as Router, Routes } from 'react-router-dom'

import { RequireAuth } from '@/components/auth/RequireAuth'
import { RequireRoleRoute } from '@/components/auth/RequireRoleRoute'
import { AppShell } from '@/components/shell/AppShell'
import { AuthProvider } from '@/context/AuthContext'
import { ToastProvider } from '@/context/ToastContext'
import { DashboardPage } from '@/pages/DashboardPage'
import { DeclarationStubPage } from '@/pages/DeclarationStubPage'
import { LoginPage } from '@/pages/LoginPage'
import { UploadPage } from '@/pages/UploadPage'

function App() {
  return (
    <Router>
      <AuthProvider>
        <ToastProvider>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route element={<RequireAuth />}>
              <Route element={<AppShell />}>
                <Route path="/" element={<DashboardPage />} />
                <Route path="/declarations/:id" element={<DeclarationStubPage />} />
                <Route element={<RequireRoleRoute role="Admin" />}>
                  <Route path="/declarations/new" element={<UploadPage />} />
                </Route>
              </Route>
            </Route>
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </ToastProvider>
      </AuthProvider>
    </Router>
  )
}

export default App
