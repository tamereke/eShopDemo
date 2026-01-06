import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.jsx'

window.onerror = function (message, source, lineno, colno, error) {
  const root = document.getElementById('root');
  if (root) {
    root.innerHTML = `<div style="padding: 2rem; color: #ef4444; font-family: sans-serif;">
      <h2>Client-Side Error Detected</h2>
      <pre style="background: #1e293b; padding: 1rem; border-radius: 0.5rem; overflow: auto; color: #f8fafc;">${message}</pre>
    </div>`;
  }
  return false;
};

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
