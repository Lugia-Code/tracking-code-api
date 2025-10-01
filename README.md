````markdown
# Tracking Code API - Sistema de Rastreamento de Motocicletas

---

## 👥 Integrantes

| Nome | RM |
| :--- | :--- |
| [Nathália Gomes da Silva] | RM554945 |
| [Nathan Magno Gustavo Consolo] | RM558987 |
| [Júlio César Nunes Oliveira] | RM557774 |

---

## 📋 Justificativa da Arquitetura

### Domínio Escolhido

**Sistema de Rastreamento de Motocicletas via RFID**

Este sistema foi desenvolvido para gerenciar o rastreamento de **motocicletas corporativas** através de tags **RFID** em diferentes setores de uma empresa ou organização. O domínio foi escolhido por representar um caso de uso real e relevante para gestão de frotas e controle de ativos.

### Entidades Principais

| Entidade | Atributos Principais | Justificativa |
| :--- | :--- | :--- |
| **Moto** | Chassi (PK), Placa, Modelo, DataCadastro, IdSetor (FK), CodigoTag (FK) | Núcleo do sistema, contém as informações essenciais do veículo |
| **Tag** | CodigoTag (PK), Status, DataVinculo, Chassi (FK *nullable*) | Gerencia os dispositivos de rastreamento e seu ciclo de vida (ativo/inativo) |
| **Setor** | IdSetor (PK), Nome | Organiza espacialmente os veículos facilitando controle e gestão |
| **Usuario** | IdFuncionario (PK), Email, Senha, Funcao | Controle de acesso e auditoria das operações |

### Padrão Arquitetural

**Minimal API com Clean Architecture simplificada** (baseado em .NET 8/9):

* **Camada de Apresentação:** Endpoints HTTP com validações e transformações.
* **Camada de Aplicação:** DTOs para contratos de entrada/saída da API.
* **Camada de Domínio:** Entidades com regras de negócio encapsuladas.
* **Camada de Infraestrutura:** `DbContext` (EF Core) para persistência em **Oracle**.

### Recursos Implementados

* **HATEOAS** para hipermídia (navegabilidade da API).
* **Paginação** em coleções grandes.
* **Health Checks** para monitoramento.
* **Rate Limiting** para proteção contra abuso.
* **Idempotência** em operações críticas (ex: criação de usuário).
* **OpenAPI/Swagger** (com Scalar) para documentação interativa.
* **Versionamento** de API (via URL, `/api/v1/`).

---

## 🚀 Instruções de Execução

### Pré-requisitos

* **.NET 8.0 SDK** (ou superior)
* **Oracle Database 19c** (ou superior, ou Oracle XE para desenvolvimento)
* **Visual Studio 2022**, Rider ou VS Code com C# extension

### Configuração do Ambiente

1.  **Clone o repositório**
    ```bash
    git clone <https://github.com/Lugia-Code/tracking-code-api.git>
    cd tracking-code-api
    ```

2.  **Configure a Connection String**
    Edite o arquivo `appsettings.json` ou `appsettings.Development.json` e defina a string de conexão do Oracle:
    ```json
    {
      "ConnectionStrings": {
        "FiapOracleDb": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XEPDB1)));User Id=seu_usuario;Password=sua_senha;"
      }
    }
    ```

3.  **Execute as Migrations**
    ```bash
    dotnet ef database update
    ```
    

4.  **Execute a aplicação**
    ```bash
    dotnet run
    ```

### Acessando a API

| Recurso | URL Padrão |
| :--- | :--- |
| **API Base URL** | `[https://localhost:7xxx](http://localhost:5117/)` ou  |
| **Documentação Swagger (Scalar)** | `[https://localhost:7xxx](http://localhost:5117/scalar)/scalar/v1` (*Apenas em $\text{Development}$*) |
| **Health Check** | `[https://localhost:7xxx](http://localhost:5117/scalar)/health` |
| **Health Dashboard** | `[https://localhost:7xxx](http://localhost:5117/scalar)/health-ui` |

---

## 📚 Exemplos de Uso dos Endpoints

### 🏍️ Motos

| Ação | Método | Endpoint | Observações |
| :--- | :--- | :--- | :--- |
| Listar todas | `GET` | `/api/v1/motos?page=1&pageSize=10` | Inclui paginação e links HATEOAS. |
| Buscar por chassi | `GET` | `/api/v1/motos/buscar/chassi/9BWZZZ377VT004251` | |
| Buscar por placa | `GET` | `/api/v1/motos/buscar/placa/ABC1234` | |
| Listar por setor | `GET` | `/api/v1/motos/setor/1?page=1&pageSize=10` | |
| **Criar nova moto** | `POST` | `/api/v1/motos` | **(ADICIONE PRINT DA RESPONSE DO SCALAR AQUI)** |
| **Atualizar moto** | `PUT` | `/api/v1/motos/9BWZZZ377VT004251` | **(ADICIONE PRINT DA RESPONSE DO SCALAR AQUI)** |
| Vincular nova tag | `PUT` | `/api/v1/motos/9BWZZZ377VT004251/tag` | Body: `{"codigoTag": "TAG002"}` |
| Desvincular tag | `PATCH` | `/api/v1/motos/9BWZZZ377VT004251/desvincular-tag` | |
| **Deletar moto** | `DELETE` | `/api/v1/motos/9BWZZZ377VT004251` | **(ADICIONE PRINT DA RESPONSE DO SCALAR AQUI)** |

#### Exemplo de Resposta de Lista (200 OK)
```json
{
  "data": [
    {
      "chassi": "9BWZZZ377VT004251",
      "placa": "ABC1234",
      "modelo": "Honda CG 160 Fan",
      "dataCadastro": "2024-01-15T10:30:00",
      "setor": {
        "idSetor": 1,
        "nome": "Garagem Principal"
      },
      "tag": {
        "codigoTag": "TAG001",
        "status": "ativo",
        "dataVinculo": "2024-01-15T10:30:00"
      }
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 10,
    "totalCount": 45,
    "totalPages": 5
  }
}
````

### 🏷️ Tags

| Ação | Método | Endpoint | Observações |
| :--- | :--- | :--- | :--- |
| Listar todas | `GET` | `/api/v1/tags` | |
| Listar disponíveis | `GET` | `/api/v1/motos/tags-disponiveis` | Tags sem vínculo com motos. |
| Buscar específica | `GET` | `/api/v1/tags/TAG001` | |
| **Criar nova tag** | `POST` | `/api/v1/tags` | **(ADICIONE PRINT DA RESPONSE DO SCALAR AQUI)** |
| **Atualizar tag** | `PUT` | `/api/v1/tags/TAG003` | Só altera se a tag não estiver vinculada. **(ADICIONE PRINT DA RESPONSE DO SCALAR AQUI)** |
| **Deletar tag** | `DELETE` | `/api/v1/tags/TAG003` | Só deleta se a tag não estiver vinculada. **(ADICIONE PRINT DA RESPONSE DO SCALAR AQUI)** |

### 🏢 Setores

| Ação | Método | Endpoint | Observações |
| :--- | :--- | :--- | :--- |
| Listar todos | `GET` | `/setores` | |
| Buscar específico | `GET` | `/setores/1` | |
| **Criar novo setor** | `POST` | `/setores` | **(ADICIONE PRINT DA RESPONSE DO SCALAR AQUI)** |
| **Atualizar setor** | `PUT` | `/setores/3` | **(ADICIONE PRINT DA RESPONSE DO SCALAR AQUI)** |
| **Deletar setor** | `DELETE` | `/setores/3` | **(ADICIONE PRINT DA RESPONSE DO SCALAR AQUI)** |

### 👤 Usuários

| Ação | Método | Endpoint | Observações |
| :--- | :--- | :--- | :--- |
| Listar todos | `GET` | `/usuarios` | |
| Buscar específico | `GET` | `/usuarios/1` | |
| **Criar novo usuário** | `POST` | `/usuarios` | Usa o header `Idempotency-Key` para garantir que a operação não seja duplicada. **(ADICIONE PRINT DA RESPONSE DO SCALAR AQUI)** |
| **Atualizar usuário** | `PUT` | `/usuarios/1` | **(ADICIONE PRINT DA RESPONSE DO SCALAR AQUI)** |
| **Deletar usuário** | `DELETE` | `/usuarios/1` | **(ADICIONE PRINT DA RESPONSE DO SCALAR AQUI)** |

-----

## 🧪 Testes

O projeto possui **12 testes automatizados** para validar os endpoints da API.

### Execução

Para executar todos os testes, use o comando:

```bash
cd tracking_code_api.Tests
dotnet test
```

Para mais detalhes, use:

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Cobertura de Testes

| Arquivo de Teste | Qtd. Testes | Escopo |
| :--- | :--- | :--- |
| `MotoEndpointsTests` | 6 testes | GET endpoints, paginação e validações. |
| `TagEndpointsTests` | 3 testes | CRUD básico de Tags. |
| `SetorEndpointsTests` | 3 testes | CRUD básico de Setores. |
| **Total** | **12 testes** | |

Os testes validam:

  * Operações `GET` em todos os endpoints.
  * Códigos de status HTTP corretos ($\text{200, 201, 204, 400, 404}$).
  * Estrutura de resposta da API e Paginação.

-----

## 📊 Health Checks

O sistema utiliza **Health Checks** para monitoramento.

### Endpoint de Saúde

```bash
GET /health
```

#### Resposta Saudável (200 OK)

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567",
  "entries": {
    "OracleDb-check": {
      "status": "Healthy",
      "duration": "00:00:00.0123456"
    }
  }
}
```

### Dashboard

Acesse o dashboard de Health Checks para uma visão amigável:
`https://localhost:7xxx/health-ui`

-----

## 🔒 Rate Limiting

A API possui **Rate Limiting** configurado para proteção contra abuso.

  * **Limite:** 10 requisições por minuto por IP.
  * **Resposta quando excedido ($\text{429 Too Many Requests}$):**
    > Muitas requisições, tente novamente em 60 segundos

Para desabilitar em desenvolvimento, comente a linha no `Program.cs`:

```csharp
// app.UseRateLimiter();
```

-----

## 📖 Tecnologias Utilizadas

  * **.NET 9.0:** Framework principal
  * **Entity Framework Core:** ORM para acesso a dados
  * **Oracle Database:** Banco de dados relacional
  * **Swagger/Scalar:** Documentação interativa da API
  * **IdempotentAPI:** Biblioteca para idempotência
  * **AspNetCore.HealthChecks:** Monitoramento de saúde
  * **Asp.Versioning:** Versionamento de API
  * **xUnit:** Framework de testes
  * **FluentAssertions:** Asserções fluentes para testes

-----

## 🎯 Conceitos REST Implementados

| Conceito | Status | Descrição |
| :--- | :--- | :--- |
| **Recursos bem definidos** | ✅ | URIs claras e semânticas. |
| **Verbos HTTP corretos** | ✅ | Uso de $\text{GET, POST, PUT, PATCH, DELETE}$. |
| **Status codes apropriados** | ✅ | Uso de $\text{200, 201, 204, 400, 404, 429, 500}$. |
| **HATEOAS** | ✅ | Uso de *Hypermedia as the Engine of Application State*. |
| **Paginação** | ✅ | Para coleções grandes de recursos. |
| **Idempotência** | ✅ | Em operações críticas (ex: $\text{POST}$ de usuário). |
| **Versionamento** | ✅ | Via URL e headers ($\text{/api/v1/}$). |
| **Content negotiation** | ✅ | JSON como formato principal. |
| **Stateless** | ✅ | Sem sessão no servidor. |

-----

## 📝 Licença

Este projeto foi desenvolvido para fins acadêmicos na **FIAP**.
