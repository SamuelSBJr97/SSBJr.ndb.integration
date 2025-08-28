#!/usr/bin/env node

import { execSync } from 'child_process';
import fs from 'fs';
import path from 'path';

const API_URL = process.env.API_URL || 'http://localhost:8080';
const OUTPUT_DIR = 'src/api/generated';

async function generateApiClient() {
    try {
        console.log('?? Gerando cliente da API...');
        
        // Criar diretório de saída se não existir
        if (!fs.existsSync(OUTPUT_DIR)) {
            fs.mkdirSync(OUTPUT_DIR, { recursive: true });
        }
        
        // Gerar cliente TypeScript usando OpenAPI Generator
        const command = `npx @openapitools/openapi-generator-cli generate \\
            -i ${API_URL}/swagger/v1/swagger.json \\
            -g typescript-axios \\
            -o ${OUTPUT_DIR} \\
            --additional-properties=useSingleRequestParameter=true,withInterfaces=true,withSeparateModelsAndApi=true`;
        
        execSync(command, { stdio: 'inherit' });
        
        console.log('? Cliente da API gerado com sucesso!');
        
        // Criar arquivo de configuração do cliente
        const configContent = `import { Configuration, DefaultApi } from './generated';

const configuration = new Configuration({
    basePath: process.env.REACT_APP_API_URL || '/api',
});

export const apiClient = new DefaultApi(configuration);
export { Configuration };
`;
        
        fs.writeFileSync(path.join(OUTPUT_DIR, 'client.ts'), configContent);
        
        console.log('? Configuração do cliente criada!');
        
    } catch (error) {
        console.error('? Erro ao gerar cliente da API:', error.message);
        console.log('??  Certifique-se de que a API está rodando e acessível.');
        process.exit(1);
    }
}

generateApiClient();