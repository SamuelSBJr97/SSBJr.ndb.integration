# SSBJr.ndb.integration.React

> React SPA - Interface moderna e responsiva para gerenciamento de APIs

## ?? Visão Geral

Aplicação **React** moderna construída com **Vite** e **TypeScript**, oferecendo uma interface alternativa para o sistema de gerenciamento de APIs. Focada em performance, UX moderna e desenvolvimento rápido.

## ??? Arquitetura

```
SSBJr.ndb.integration.React/
??? src/
?   ??? components/         # Componentes React
?   ?   ??? ui/            # Componentes de UI base
?   ?   ??? forms/         # Formulários
?   ?   ??? charts/        # Componentes de gráficos
?   ??? pages/             # Páginas da aplicação
?   ??? services/          # Serviços API
?   ??? hooks/             # Custom hooks
?   ??? context/           # Context providers
?   ??? types/             # TypeScript types
?   ??? utils/             # Utilitários
?   ??? styles/            # Estilos CSS/SCSS
??? public/                # Assets públicos
??? vite.config.js         # Configuração Vite
??? package.json           # Dependencies
??? Dockerfile             # Container config
```

## ?? Funcionalidades

### ? Performance
- **Vite** - Build tool ultra-rápido
- **Code splitting** automático
- **Tree shaking** otimizado
- **Hot Module Replacement** (HMR)
- **Lazy loading** de componentes

### ?? UI/UX Moderna
- **Design responsivo** mobile-first
- **Dark/Light mode** toggle
- **Smooth animations** com Framer Motion
- **Loading states** inteligentes
- **Toast notifications**
- **Skeleton loading**

### ?? Integração API
- **Axios** para HTTP requests
- **SWR/TanStack Query** para cache de dados
- **SignalR** para real-time updates
- **Retry policies** automáticas
- **Error boundaries**

### ?? PWA Ready
- **Service Worker** para offline
- **App installable**
- **Background sync**
- **Push notifications**

## ??? Tecnologias

### Core
- **React 19** - Framework UI
- **TypeScript** - Type safety
- **Vite** - Build tool e dev server
- **React Router** - Client-side routing

### UI/Styling
- **Tailwind CSS** - Utility-first CSS
- **Headless UI** - Unstyled components
- **Heroicons** - Icon library
- **Framer Motion** - Animations
- **Chart.js/Recharts** - Data visualization

### Estado e Dados
- **Zustand** - State management
- **TanStack Query** - Server state
- **React Hook Form** - Forms
- **Zod** - Schema validation

### Comunicação
- **Axios** - HTTP client
- **Microsoft SignalR** - Real-time
- **Socket.io** - WebSocket fallback

### Dev Tools
- **ESLint** - Code linting
- **Prettier** - Code formatting
- **Vitest** - Unit testing
- **Playwright** - E2E testing
- **Storybook** - Component documentation

## ?? Como Executar

### Desenvolvimento
```bash
# Instalar dependências
npm install

# Executar dev server
npm run dev

# Acesso: http://localhost:3000
```

### Via Script PowerShell
```powershell
# Executar apenas React
.\Run-React.ps1

# Ou com limpeza
.\Run-React.ps1 -Clean -Install
```

### Build para Produção
```bash
# Build otimizado
npm run build

# Preview do build
npm run preview

# Servir build
npx serve -s dist
```

### Docker
```bash
# Build da imagem
docker build -t ssbjr-react-app .

# Executar container
docker run -p 3000:80 ssbjr-react-app
```

## ?? Configuração

### Vite Config
```javascript
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { resolve } from 'path';

export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0',
    port: 3000,
    strictPort: true,
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
});
```

### Environment Variables
```bash
# .env.local
VITE_API_BASE_URL=https://localhost:8080
VITE_SIGNALR_HUB_URL=https://localhost:8080/notifications
VITE_APP_TITLE="SSBJr API Manager"
VITE_ENABLE_PWA=true
```

## ?? Deploy

### Via Docker
```dockerfile
# Multi-stage build
FROM node:18-alpine AS builder

WORKDIR /app
COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

# Production stage
FROM nginx:alpine
COPY nginx.conf /etc/nginx/nginx.conf
COPY --from=builder /app/dist /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

### Como Usar

#### Opção 1: Execução Manual
```bash
cd SSBJr.ndb.integration.React
npm install
npm run dev
```

#### Opção 2: Via Script PowerShell
```powershell
# Executar apenas React
.\Run-React.ps1

# Com instalação de dependências
.\Run-React.ps1 -Install

# Com build para produção
.\Run-React.ps1 -Build

# Limpeza completa
.\Run-React.ps1 -Clean -Install
```

## ?? Features Implementadas

### ? Interface Base
- Layout responsivo
- Navegação principal
- Dashboard básico

### ?? Em Desenvolvimento
- Integração completa com API
- Formulários de CRUD
- Gráficos e métricas
- Sistema de notificações

### ?? Planejado
- PWA features
- Testes automatizados
- Storybook documentation
- Performance optimization

---

*Para informações gerais do projeto, veja o [README principal](../README.md).*
