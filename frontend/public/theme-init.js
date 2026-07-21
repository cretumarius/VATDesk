// Applied before paint to avoid a flash of the wrong theme. Mirrors the logic in
// src/lib/theme.ts — keep the two in sync if the storage key/preference logic changes.
// Lives in its own file (not inline in index.html) so a strict script-src 'self' CSP
// doesn't have to allow 'unsafe-inline' just for this one snippet.
(function () {
  var stored = localStorage.getItem('vatdesk-theme')
  var isDark = stored === 'dark' || (stored !== 'light' && window.matchMedia('(prefers-color-scheme: dark)').matches)
  if (isDark) document.documentElement.classList.add('dark')
})()
