# Contribuindo para SSBJr.ndb.integration

> Obrigado por considerar contribuir para o projeto! ??

Sua contribuição é muito bem-vinda e ajuda a tornar este projeto melhor para toda a comunidade.

## ?? Como Contribuir

### 1. Fork e Clone
```bash
# Fork o projeto no GitHub
git clone https://github.com/SeuUsuario/SSBJr.ndb.integration.git
cd SSBJr.ndb.integration
```

### 2. Configurar Ambiente
```bash
# Instalar dependências
dotnet restore

# Configurar Git hooks (opcional)
git config core.hooksPath .githooks
```

### 3. Criar Branch
```bash
# Para nova funcionalidade
git checkout -b feature/nova-funcionalidade

# Para correção de bug
git checkout -b fix/correcao-bug

# Para documentação
git checkout -b docs/melhorar-documentacao
```

### 4. Fazer Mudanças
- Siga os [padrões de código](#padrões-de-código)
- Adicione testes quando apropriado
- Atualize documentação se necessário

### 5. Commit
```bash
git add .
git commit -m "feat: adiciona nova funcionalidade X"
```

### 6. Push e Pull Request
```bash
git push origin feature/nova-funcionalidade
```

Então abra um Pull Request no GitHub com:
- Descrição clara do que foi mudado
- Screenshots se aplicável
- Referência a issues relacionadas

## ?? Padrões de Código

### C# (.NET)
```csharp
// ? Bom
public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;

    public async Task<List<ApiInterface>> GetAllAsync()
    {
        // Implementation
    }
}

// ? Evitar
public class apiservice
{
    public List<ApiInterface> GetAll()
    {
        // Synchronous implementation
    }
}
```

### Convenções de Nomenclatura
- **Classes**: PascalCase (`ApiService`, `UserManager`)
- **Métodos**: PascalCase (`GetAllAsync`, `CreateAsync`)
- **Propriedades**: PascalCase (`Name`, `CreatedAt`)
- **Campos privados**: _camelCase (`_httpClient`, `_logger`)
- **Parâmetros**: camelCase (`userId`, `apiInterface`)

### Async/Await
```csharp
// ? Sempre async para I/O
public async Task<ApiInterface> CreateAsync(CreateRequest request)
{
    var response = await _httpClient.PostAsJsonAsync("/api", request);
    return await response.Content.ReadFromJsonAsync<ApiInterface>();
}
```

### React/TypeScript
```typescript
// ? Functional Components com TypeScript
interface Props {
  apiInterface: ApiInterface;
  onEdit: (api: ApiInterface) => void;
}

export const ApiCard: React.FC<Props> = ({ apiInterface, onEdit }) => {
  return (
    <div className="card">
      <h3>{apiInterface.name}</h3>
    </div>
  );
};
```

## ?? Testes

### Executar Testes
```bash
# Todos os testes
dotnet test

# Apenas testes unitários
dotnet test --filter Category=Unit

# Com cobertura
dotnet test --collect:"XPlat Code Coverage"
```

### Escrever Testes
```csharp
[Test]
public async Task GetAllAsync_ShouldReturnApiInterfaces()
{
    // Arrange
    var mockHttp = new Mock<HttpMessageHandler>();
    // ... setup mock

    // Act
    var result = await _apiService.GetAllAsync();

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(2);
}
```

## ?? Convenções de Commit

Usamos [Conventional Commits](https://www.conventionalcommits.org/):

### Tipos
- `feat`: Nova funcionalidade
- `fix`: Correção de bug
- `docs`: Mudanças na documentação
- `style`: Formatting, missing semi colons, etc
- `refactor`: Code refactoring
- `test`: Adicionando testes
- `chore`: Updating build tasks, etc

### Exemplos
```bash
feat: adiciona suporte a GraphQL subscriptions
fix: corrige erro de autenticação no login
docs: atualiza README com instruções Docker
style: aplica formatação no ApiService
refactor: extrai lógica de validação para service separado
test: adiciona testes para AuthService
chore: atualiza dependências do projeto
```

### Escopo (opcional)
```bash
feat(api): adiciona endpoint de métricas
fix(web): corrige layout responsivo no mobile
docs(readme): atualiza seção de instalação
```

## ?? Reportando Bugs

### Antes de Reportar
1. Verifique se já existe issue similar
2. Teste na versão mais recente
3. Colete informações do sistema

### Template de Bug Report
```markdown
**Descrição**
Descrição clara do bug.

**Para Reproduzir**
1. Vá para '...'
2. Clique em '....'
3. Role para baixo até '....'
4. Veja o erro

**Comportamento Esperado**
O que deveria acontecer.

**Screenshots**
Se aplicável, adicione screenshots.

**Ambiente:**
 - OS: [e.g. Windows 11]
 - Browser: [e.g. Chrome 91]
 - .NET Version: [e.g. 8.0]
 - Versão do projeto: [e.g. 1.0.0]

**Informação Adicional**
Qualquer outra informação sobre o problema.
```

## ?? Sugerindo Melhorias

### Feature Requests
Para sugerir uma nova funcionalidade:

1. **Verifique roadmap** e issues existentes
2. **Abra uma Discussion** para discutir a ideia
3. **Detalhe o caso de uso** e benefícios
4. **Considere implementar** você mesmo

### Template de Feature Request
```markdown
**Problema que Resolve**
Descrição do problema atual ou limitação.

**Solução Proposta**
Descrição clara da funcionalidade desejada.

**Alternativas Consideradas**
Outras soluções que você considerou.

**Contexto Adicional**
Screenshots, mockups, ou informações relevantes.
```

## ?? Code Review

### Como Reviewer
- **Seja construtivo** e educativo
- **Foque no código**, não na pessoa
- **Sugira melhorias** específicas
- **Reconheça boas práticas**
- **Teste localmente** quando necessário

### Como Contribuidor
- **Responda feedback** de forma profissional
- **Faça mudanças solicitadas** prontamente
- **Explique decisões** técnicas quando necessário
- **Mantenha PRs focados** e pequenos

### Checklist de Review
- [ ] Código segue padrões do projeto
- [ ] Testes adequados incluídos
- [ ] Documentação atualizada
- [ ] Sem breaking changes não documentadas
- [ ] Performance considerada
- [ ] Segurança verificada

## ?? Recursos Úteis

### Documentação
- [README Principal](README.md)
- [Guia de Arquitetura](docs/development/architecture.md)
- [API Documentation](docs/api/)

### Ferramentas
- [Visual Studio 2022](https://visualstudio.microsoft.com/)
- [VS Code](https://code.visualstudio.com/)
- [Git](https://git-scm.com/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

### Aprendizado
- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [Blazor Documentation](https://docs.microsoft.com/aspnet/core/blazor/)
- [MAUI Documentation](https://docs.microsoft.com/dotnet/maui/)
- [React Documentation](https://reactjs.org/docs/)

## ??? Reconhecimento

Contribuidores são reconhecidos em:
- [Contributors](https://github.com/SamuelSBJr97/SSBJr.ndb.integration/contributors) do GitHub
- Changelog do projeto
- README principal (para contribuições significativas)

## ? Precisa de Ajuda?

### Canais de Suporte
- **GitHub Issues** - Para bugs e feature requests
- **GitHub Discussions** - Para dúvidas e discussões gerais
- **Email** - samuel.sbj97@gmail.com para questões específicas

### Primeiros Passos
Se você é novo em contribuições open source:
1. Comece com [good first issues](https://github.com/SamuelSBJr97/SSBJr.ndb.integration/labels/good%20first%20issue)
2. Leia a [documentação](docs/)
3. Participe das [discussões](https://github.com/SamuelSBJr97/SSBJr.ndb.integration/discussions)

## ?? Código de Conduta

Este projeto adota o [Código de Conduta do Contributor Covenant](CODE_OF_CONDUCT.md). 
Ao participar, você deve seguir este código.

---

**Obrigado por contribuir! ??**

Sua participação torna este projeto melhor para todos na comunidade.