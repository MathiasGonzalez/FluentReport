import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

const repositoryName = process.env.GITHUB_REPOSITORY?.split('/')[1]
const githubPagesBasePath = repositoryName ? `/${repositoryName}/` : '/'

function manualChunks(id: string) {
  if (!id.includes('node_modules')) {
    return undefined
  }

  if (id.includes('react-moveable') || id.includes('react-selecto') || id.includes('@scena/')) {
    return 'editor-vendor'
  }

  if (id.includes('react') || id.includes('scheduler')) {
    return 'react-vendor'
  }

  return 'vendor'
}

// https://vite.dev/config/
export default defineConfig({
  base: process.env.VITE_BASE_PATH ?? (process.env.GITHUB_ACTIONS ? githubPagesBasePath : '/'),
  plugins: [react()],
  build: {
    rollupOptions: {
      output: {
        manualChunks,
      },
    },
  },
})
