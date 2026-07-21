import { useEffect, useState } from 'react'
import { getHealth } from './api/client'
import type { HealthResponse } from './api/types'
import './App.css'

type Status =
  | { state: 'loading' }
  | { state: 'error'; message: string }
  | { state: 'success'; data: HealthResponse }

function App() {
  const [status, setStatus] = useState<Status>({ state: 'loading' })

  useEffect(() => {
    getHealth()
      .then((data) => setStatus({ state: 'success', data }))
      .catch((error: Error) => setStatus({ state: 'error', message: error.message }))
  }, [])

  return (
    <main className="app">
      <h1>VATDesk</h1>
      <p className="subtitle">Hungarian VAT declaration generator</p>

      <section className="health-card">
        <h2>API health</h2>
        {status.state === 'loading' && <p>Checking API…</p>}
        {status.state === 'error' && <p className="error">Error: {status.message}</p>}
        {status.state === 'success' && (
          <ul>
            <li>Version: {status.data.version}</li>
            <li>
              Database:{' '}
              <span className={status.data.databaseConnected ? 'ok' : 'error'}>
                {status.data.databaseConnected ? 'connected' : 'disconnected'}
              </span>
            </li>
          </ul>
        )}
      </section>
    </main>
  )
}

export default App
