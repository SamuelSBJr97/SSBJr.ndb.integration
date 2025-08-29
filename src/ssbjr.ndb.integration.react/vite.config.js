import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin()],
    server: {
        host: '0.0.0.0', // Permite acesso externo
        port: 3000,
        strictPort: true, // Falha se a porta não estiver disponível
        hmr: {
            port: 3000
        }
    },
    build: {
        outDir: 'dist',
        assetsDir: 'assets',
        sourcemap: false,
        minify: 'esbuild'
    },
    base: '/'
})