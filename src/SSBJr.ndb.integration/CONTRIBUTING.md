# Contribuindo para SSBJr.ndb.integration

> Obrigado por considerar contribuir para o projeto! ??

Sua contribui��o � muito bem-vinda e ajuda a tornar este projeto melhor para toda a comunidade.

## ?? Como Contribuir

### 1. Fork e Clone
```bash
# Fork o projeto no GitHub
git clone https://github.com/SeuUsuario/SSBJr.ndb.integration.git
cd SSBJr.ndb.integration
```

### 2. Configurar Ambiente
```bash
# Instalar depend�ncias
dotnet restore

# Configurar Git hooks (opcional)
git config core.hooksPath .githooks
```

### 3. Criar Branch
```bash
# Para nova funcionalidade
git checkout -b feature/nova-funcionalidade

# Para corre��o de bug
git checkout -b fix/correcao-bug

# Para documenta��o
git checkout -b docs/melhorar-documentacao
```

### 4. Fazer Mudan�as
- Siga os [padr�es de c�digo](#padr�es-de-c�digo)
- Adicione testes quando apropriado
- Atualize documenta��o se necess�rio

### 5. Commit
```bash
git add .
git commit -m "feat: adiciona nova funcionalidade X"
```

### 6. Push e Pull Request
```bash
git push origin feature/nova-funcionalidade
```

Ent�o abra um Pull Request no GitHub com:
- Descri��o clara do que foi mudado
- Screenshots se aplic�vel
- Refer�ncia a issues relacionadas

## ?? Padr�es de C�digo

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

### Conven��es de Nomenclatura
- **Classes**: PascalCase (`ApiService`, `UserManager`)
- **M�todos**: PascalCase (`GetAllAsync`, `CreateAsync`)
- **Propriedades**: PascalCase (`Name`, `CreatedAt`)
- **Campos privados**: _camelCase (`_httpClient`, `_logger`)
- **Par�metros**: camelCase (`userId`, `apiInterface`)

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

# Apenas testes unit�rios
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

## ?? Conven��es de Commit

Usamos [Conventional Commits](https://www.conventionalcommits.org/):

### Tipos
- `feat`: Nova funcionalidade
- `fix`: Corre��o de bug
- `docs`: Mudan�as na documenta��o
- `style`: Formatting, missing semi colons, etc
- `refactor`: Code refactoring
- `test`: Adicionando testes
- `chore`: Updating build tasks, etc

### Exemplos
```bash
feat: adiciona suporte a GraphQL subscriptions
fix: corrige erro de autentica��o no login
docs: atualiza README com instru��es Docker
style: aplica formata��o no ApiService
refactor: extrai l�gica de valida��o para service separado
test: adiciona testes para AuthService
chore: atualiza depend�ncias do projeto
```

### Escopo (opcional)
```bash
feat(api): adiciona endpoint de m�tricas
fix(web): corrige layout responsivo no mobile
docs(readme): atualiza se��o de instala��o
```

## ?? Reportando Bugs

### Antes de Reportar
1. Verifique se j� existe issue similar
2. Teste na vers�o mais recente
3. Colete informa��es do sistema

### Template de Bug Report
```markdown
**Descri��o**
Descri��o clara do bug.

**Para Reproduzir**
1. V� para '...'
2. Clique em '....'
3. Role para baixo at� '....'
4. Veja o erro

**Comportamento Esperado**
O que deveria acontecer.

**Screenshots**
Se aplic�vel, adicione screenshots.

**Ambiente:**
 - OS: [e.g. Windows 11]
 - Browser: [e.g. Chrome 91]
 - .NET Version: [e.g. 8.0]
 - Vers�o do projeto: [e.g. 1.0.0]

**Informa��o Adicional**
Qualquer outra informa��o sobre o problema.
```

## ?? Sugerindo Melhorias

### Feature Requests
Para sugerir uma nova funcionalidade:

1. **Verifique roadmap** e issues existentes
2. **Abra uma Discussion** para discutir a ideia
3. **Detalhe o caso de uso** e benef�cios
4. **Considere implementar** voc� mesmo

### Template de Feature Request
```markdown
**Problema que Resolve**
Descri��o do problema atual ou limita��o.

**Solu��o Proposta**
Descri��o clara da funcionalidade desejada.

**Alternativas Consideradas**
Outras solu��es que voc� considerou.

**Contexto Adicional**
Screenshots, mockups, ou informa��es relevantes.
```

## ?? Code Review

### Como Reviewer
- **Seja construtivo** e educativo
- **Foque no c�digo**, n�o na pessoa
- **Sugira melhorias** espec�ficas
- **Reconhe�a boas pr�ticas**
- **Teste localmente** quando necess�rio

### Como Contribuidor
- **Responda feedback** de forma profissional
- **Fa�a mudan�as solicitadas** prontamente
- **Explique decis�es** t�cnicas quando necess�rio
- **Mantenha PRs focados** e pequenos

### Checklist de Review
- [ ] C�digo segue padr�es do projeto
- [ ] Testes adequados inclu�dos
- [ ] Documenta��o atualizada
- [ ] Sem breaking changes n�o documentadas
- [ ] Performance considerada
- [ ] Seguran�a verificada

## ?? Recursos �teis

### Documenta��o
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

Contribuidores s�o reconhecidos em:
- [Contributors](https://github.com/SamuelSBJr97/SSBJr.ndb.integration/contributors) do GitHub
- Changelog do projeto
- README principal (para contribui��es significativas)

## ? Precisa de Ajuda?

### Canais de Suporte
- **GitHub Issues** - Para bugs e feature requests
- **GitHub Discussions** - Para d�vidas e discuss�es gerais
- **Email** - samuel.sbj97@gmail.com para quest�es espec�ficas

### Primeiros Passos
Se voc� � novo em contribui��es open source:
1. Comece com [good first issues](https://github.com/SamuelSBJr97/SSBJr.ndb.integration/labels/good%20first%20issue)
2. Leia a [documenta��o](docs/)
3. Participe das [discuss�es](https://github.com/SamuelSBJr97/SSBJr.ndb.integration/discussions)

## ?? C�digo de Conduta

Este projeto adota o [C�digo de Conduta do Contributor Covenant](CODE_OF_CONDUCT.md). 
Ao participar, voc� deve seguir este c�digo.

---

**Obrigado por contribuir! ??**

Sua participa��o torna este projeto melhor para todos na comunidade.