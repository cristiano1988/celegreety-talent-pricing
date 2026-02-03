import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [vue()],
    server: {
        host: true,
        port: 5173,
        proxy: {
            '/api': {
                target: 'http://localhost:5000', // Local dev target
                changeOrigin: true,
                secure: false
            }
        }
    },
    preview: {
        port: 4173,
        proxy: {
            '/api': {
                target: process.env.VITE_API_URL || 'http://backend:8080', // Docker target
                changeOrigin: true
            }
        }
    }
})
