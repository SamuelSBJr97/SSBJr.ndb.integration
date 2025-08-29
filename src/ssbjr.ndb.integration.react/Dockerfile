# Dockerfile para React App
FROM node:18-alpine AS builder

# Instalar ferramentas necessárias
RUN apk add --no-cache git

WORKDIR /app

# Copiar package.json e package-lock.json (se existir)
COPY package.json ./
COPY package-lock.json* ./

# Configurar npm para ignorar scripts de pós-instalação problemáticos
RUN npm config set ignore-scripts true

# Instalar dependências
RUN npm install

# Resetar configuração
RUN npm config delete ignore-scripts

# Copiar código fonte
COPY . .

# Tentar gerar client da API (pode falhar, mas não deve parar o build)
RUN npm run generate-client || echo "Skipping API client generation"

# Build da aplicação
RUN npm run build

# Verificar se o diretório dist foi criado
RUN ls -la /app/ && ls -la /app/dist/ || echo "Build directory not found"

# Estágio de produção com nginx
FROM nginx:alpine

# Copiar configuração customizada do nginx
COPY nginx.conf /etc/nginx/nginx.conf

# Copiar build da aplicação
COPY --from=builder /app/dist /usr/share/nginx/html

# Expor porta 80
EXPOSE 80

# Comando padrão
CMD ["nginx", "-g", "daemon off;"]