import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { fileURLToPath, URL } from 'node:url'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5172,
    open: true,
    proxy: {
      '/api': {
        target: process.env.VITE_BACKEND_URL || 'http://localhost:5135',
        changeOrigin: true,
        secure: false,
      },
      '/health': {
        target: process.env.VITE_BACKEND_URL || 'http://localhost:5135',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
