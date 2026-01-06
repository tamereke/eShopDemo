import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: process.env.PORT ? parseInt(process.env.PORT) : 5173,
    proxy: {
      '/api': {
        target: process.env.services__gateway__http__0 || 'http://localhost:5209',
        changeOrigin: true,
        rewrite: (path) => path
      },
      '/eventhub': {
        target: process.env.services__gateway__http__0 || 'http://localhost:5209',
        changeOrigin: true,
        ws: true
      }
    }
  }
})
