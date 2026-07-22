# Guia de Implementação de Segurança (JWT e RBAC) no .NET C#

Este documento descreve os passos e padrões utilizados para implementar autenticação e autorização baseada em Roles (RBAC - Role Based Access Control) com JWT, alinhado à **Clean Architecture** e **SOLID**.

## 1. Bibliotecas Utilizadas
- `Microsoft.AspNetCore.Authentication.JwtBearer` - Para a leitura e validação dos tokens JWT.
- `BCrypt.Net-Next` - Para criptografia (hash) profissional de senhas. Nunca salvamos a senha em texto puro!

## 2. A Entidade de Usuário e Roles (RBAC)
Para não trabalharmos com mocks (dados falsos hardcoded), criamos uma entidade real `Usuario` e um *Enum* `TipoUsuario` (Roles).

1. O `Usuario` possui as propriedades: `Nome`, `Email`, `Senha` (hasheada), `Cpf`, `NumeroCelular` e `Role` (`Cliente`, `Administrador`, `Funcionario`).
2. Atualizamos o `AppDbContext` para conter o `DbSet<Usuario> Usuarios` e injetamos um usuário "Administrador" de semente (seed data) no SQLite ao rodar a migration.

## 3. Padrões de Projeto e Arquitetura

### A. Repositórios (Repository Pattern)
Para abstrair o acesso a dados do Entity Framework Core, criamos a interface `IUsuarioRepository` e a sua implementação `UsuarioRepository`. 
- **SOLID**: Respeitamos o Princípio de Inversão de Dependência (DIP) e Responsabilidade Única (SRP).

### B. Casos de Uso (Use Cases)
Seguindo o Clean Architecture, a lógica de negócio principal do login e da verificação de senha não pode ficar suja no Controller. Criamos o `LoginUseCase` (`ILoginUseCase`):
- Ele recebe o `Email` e a `Senha` da camada de interface.
- Busca o usuário através do repositório de dados.
- Verifica a senha criptografada usando o pacote **BCrypt**.
- Emite um Token válido usando nosso `TokenService` se a senha bater, inserindo a **Role** do usuário nas *Claims* do token JWT.

*Associação Node.js*: No NestJS, essa camada de UseCase normalmente estaria dentro de um `AuthService` ou em uma pasta separada de `UseCases/Commands`.

### C. Geração do Token JWT (Com Roles)
O `TokenService` adiciona o perfil/role ao Token.
```csharp
var claims = new[]
{
    new Claim(ClaimTypes.NameIdentifier, username),
    new Claim(ClaimTypes.Role, role) // <-- Injetando Administrador, Funcionario ou Cliente
};
```
Isso é fundamental, pois quando o Token chega de volta no sistema e o middleware `.UseAuthentication()` o analisa, ele injeta esse Perfil nas permissões do usuário automaticamente.

## 4. Protegendo as Rotas com base em Roles (Guards de Autorização)
- **No NestJS**: Você faria `@UseGuards(RolesGuard)` e `@Roles('Administrador')`.
- **No .NET**: A implementação nativa permite apenas colocar os perfis desejados separados por vírgula no atributo `[Authorize]`.

Adicionamos a restrição no nosso Controller:
```csharp
[Authorize(Roles = "Administrador, Funcionario")]
public class RoomAdministrationController : ControllerBase
```
Isso significa que **Clientes** logados com tokens JWT normais irão receber um **Erro 403 (Forbidden)** ao tentar acessar os endpoints de Administração, mas os **Administradores** e **Funcionarios** poderão passar (pois eles recebem acesso total baseando-se no papel assinado pelo nosso TokenService).

## 5. Como testar essa atualização?
1. Execute a aplicação.
2. Nós alimentamos o banco com um usuário de testes que tem perfil `Administrador`.
3. Bata na rota `POST /api/Auth/login` com o seguinte JSON:
   ```json
   {
     "email": "admin@hotel.com",
     "password": "admin123"
   }
   ```
4. Você receberá o `token`.
5. Coloque o token no header `Authorization: Bearer <TOKEN>` nas rotas do `RoomAdministrationController`. 
6. (Desafio) Se você criar uma rota de cadastro e cadastrar-se como `Cliente`, o token retornado não terá acesso ao controlador de administração!
