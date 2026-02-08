# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**AnswerMe** is an open-source, self-hosted intelligent question bank system that allows users to use their own AI API keys to generate questions. Users maintain full control over their learning data and can deploy to their own servers with Docker.

**Tech Stack:**
- Backend: .NET 10 Web API + EF Core 10
- Frontend: Vue 3 + TypeScript + Pinia + Element Plus
- Database: SQLite (development) / PostgreSQL (production)
- Deployment: Docker Compose

**Core Architecture:**
Clean Architecture with 4 distinct layers:
1. **Domain** (`backend/AnswerMe.Domain/`) - Entities and interfaces, no dependencies
2. **Application** (`backend/AnswerMe.Application/`) - Business logic, DTOs, services
3. **Infrastructure** (`backend/AnswerMe.Infrastructure/`) - EF Core, repositories, external concerns
4. **API** (`backend/AnswerMe.API/`) - Controllers, middleware, startup configuration

## Development Commands

### Backend (.NET 10)

```bash
# Navigate to backend
cd backend

# Build solution
dotnet build

# Run API (development, auto-applies migrations)
dotnet run --project AnswerMe.API

# Run with specific database type
set DB_TYPE=PostgreSQL
dotnet run --project AnswerMe.API

# Create new migration
dotnet ef migrations add MigrationName --project AnswerMe.Infrastructure --startup-project AnswerMe.API

# Apply migrations manually
dotnet ef database update --project AnswerMe.Infrastructure --startup-project AnswerMe.API

# Run tests
dotnet test

# Watch mode for development
dotnet watch --project AnswerMe.API
```

### Frontend (Vue 3 + Vite)

```bash
# Navigate to frontend
cd frontend/AnswerMe.Frontend

# Install dependencies
npm install

# Run dev server (http://localhost:5173)
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview
```

### Docker Development

```bash
# Start all services (db + backend + frontend)
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down

# Rebuild and start
docker-compose up -d --build
```

## Architecture Patterns

### Clean Architecture Layering

**Critical Rule:** Dependencies must flow inward only. Domain → Application → Infrastructure → API. Never reverse dependencies.

- **Domain** has ZERO dependencies on other layers. Defines entities (`User`, `QuestionBank`, `Question`, `DataSource`) and repository interfaces.
- **Application** depends on Domain. Contains services (`AuthService`, `QuestionBankService`, `AIGenerationService`) and DTOs.
- **Infrastructure** implements Domain interfaces. Contains EF Core `AnswerMeDbContext` and repository implementations.
- **API** depends on Application. Contains controllers, JWT configuration, middleware pipeline.

### Repository Pattern

All repositories are defined in `Domain/Interfaces/` and implemented in `Infrastructure/Repositories/`.

**Standard Repository Interface:**
```csharp
public interface IQuestionRepository
{
    Task<Question?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Question>> GetByQuestionBankIdAsync(int questionBankId, CancellationToken cancellationToken = default);
    Task<Question> AddAsync(Question question, CancellationToken cancellationToken = default);
    Task<Question> UpdateAsync(Question question, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
```

**Registration in Program.cs:**
```csharp
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
```

### Service Layer

Services contain business logic and are registered as scoped:
```csharp
builder.Services.AddScoped<IQuestionBankService, QuestionBankService>();
builder.Services.AddScoped<IAIGenerationService, AIGenerationService>();
```

Services should:
- Use repositories for data access (never use `DbContext` directly)
- Return DTOs, not entities
- Handle business rules and validation
- Throw `InvalidOperationException` for business rule violations

### Optimistic Locking

`QuestionBank` entity uses `byte[] Version` for optimistic locking:

```csharp
public byte[] Version { get; set; } = Array.Empty<byte>();
```

**Update logic in service:**
```csharp
if (dto.Version != null && !questionBank.Version.SequenceEqual(dto.Version))
{
    throw new InvalidOperationException("题库已被其他用户修改，请刷新后重试");
}

// Increment version
var versionBytes = questionBank.Version ?? new byte[8];
var version = BitConverter.ToInt64(versionBytes);
BitConverter.GetBytes(version + 1).CopyTo(questionBank.Version);
```

### Cursor-Based Pagination

Implemented in `QuestionBankRepository.GetPagedAsync()`:
```csharp
public async Task<List<QuestionBank>> GetPagedAsync(int userId, int pageSize, int? lastId, CancellationToken cancellationToken = default)
{
    var query = _context.QuestionBanks
        .Where(qb => qb.UserId == userId)
        .OrderByDescending(qb => qb.Id)
        .Take(pageSize + 1); // Fetch one extra to determine if more exist

    if (lastId.HasValue)
    {
        query = query.Where(qb => qb.Id < lastId.Value);
    }

    var results = await query.ToListAsync(cancellationToken);
    return results.Take(pageSize).ToList();
}
```

Returns `pageSize + 1` items. If you get `pageSize + 1` results, there are more pages.

### AI Provider Abstraction

Multiple AI providers supported through `IAIProvider` interface:

**Interface** (`Application/AI/IAIProvider.cs`):
```csharp
public interface IAIProvider
{
    string ProviderName { get; }
    Task<AIQuestionGenerateResponse> GenerateQuestionsAsync(
        AIQuestionGenerateRequest request,
        CancellationToken cancellationToken = default);
    Task<bool> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
}
```

**Factory Pattern** (`Application/AI/AIProviderFactory.cs`):
```csharp
public class AIProviderFactory
{
    public IAIProvider? GetProvider(string providerName)
    {
        return _providers.FirstOrDefault(p =>
            p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
    }
}
```

**Currently Implemented:**
- `OpenAIProvider` (`Application/AI/OpenAIProvider.cs`)

**To add new provider:**
1. Create class implementing `IAIProvider` in `Application/AI/`
2. Register in `Program.cs`: `builder.Services.AddSingleton<IAIProvider, NewProvider>();`
3. Update factory (automatic via DI)

### DataSource Entity

**Critical:** `DataSource` stores AI provider configurations with encrypted API keys:

```csharp
public class DataSource : BaseEntity
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // openai, qwen, custom_api
    public string Config { get; set; } = string.Empty; // JSON configuration (encrypted)
    public bool IsDefault { get; set; } = false;
}
```

**Config JSON structure (encrypted before storage):**
```json
{
  "apiKey": "sk-...",
  "baseUrl": "https://api.openai.com/v1",
  "model": "gpt-4"
}
```

**Encryption:** Uses ASP.NET Core Data Protection API. Never log or return API keys to frontend.

### JWT Authentication

**Configuration** (environment variable takes precedence):
```bash
JWT_SECRET=must-be-at-least-32-characters-long
JWT_ISSUER=AnswerMe
JWT_AUDIENCE=AnswerMeUsers
JWT_EXPIRY_DAYS=30
```

**User extraction in controllers:**
```csharp
private int GetCurrentUserId()
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
    {
        throw new UnauthorizedAccessException("无效的用户身份");
    }
    return userId;
}
```

### API Key Security Rules

**CRITICAL SECURITY RULES:**
1. API keys are encrypted in database using Data Protection API
2. API key endpoints NEVER return the actual key value
3. Only validation endpoints return success/failure
4. Never log API keys or include in error messages
5. Use environment variable `ENCRYPTION_KEY` for Data Protection (in production)

### Database Selection

**SQLite** (default development):
- File: `answerme_dev.db` in API project directory
- Connection string: `Data Source=answerme_dev.db`
- Auto-applied migrations on startup in development mode

**PostgreSQL** (production):
- Set environment variable: `DB_TYPE=PostgreSQL`
- Connection string from appsettings or environment

### Entity Framework Core

**DbContext** (`Infrastructure/Data/AnswerMeDbContext.cs`):
- Auto-discover entities in `Domain.Entities` namespace
- Migrations in `Infrastructure/Migrations/`
- Cascade delete configured for relationships

**Creating migrations:**
```bash
cd backend
dotnet ef migrations add AddNewField --project AnswerMe.Infrastructure --startup-project AnswerMe.API
```

**Migration auto-apply on startup** (development only):
```csharp
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AnswerMeDbContext>();
    db.Database.Migrate();
}
```

## Important Files and Locations

### Configuration Files

- **Backend config**: `backend/AnswerMe.API/appsettings.json`
- **Environment variables**: `.env.example` (root directory)
- **Docker compose**: `docker-compose.yml` (root)
- **Frontend config**: `frontend/AnswerMe.Frontend/vite.config.ts`

### Key Source Files

**Domain Layer:**
- `backend/AnswerMe.Domain/Entities/` - All entity definitions
- `backend/AnswerMe.Domain/Interfaces/` - Repository interfaces

**Application Layer:**
- `backend/AnswerMe.Application/DTOs/` - Request/response DTOs
- `backend/AnswerMe.Application/Services/` - Business logic services
- `backend/AnswerMe.Application/Interfaces/` - Service interfaces
- `backend/AnswerMe.Application/AI/` - AI provider implementations

**Infrastructure Layer:**
- `backend/AnswerMe.Infrastructure/Data/AnswerMeDbContext.cs` - EF Core context
- `backend/AnswerMe.Infrastructure/Repositories/` - Repository implementations
- `backend/AnswerMe.Infrastructure/Migrations/` - Database migrations

**API Layer:**
- `backend/AnswerMe.API/Program.cs` - Startup configuration, DI registration
- `backend/AnswerMe.API/Controllers/` - API endpoints

## Common Patterns

### Adding New Entity

1. **Create entity** in `Domain/Entities/`:
```csharp
public class MyEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    // Navigation properties
}
```

2. **Add to DbContext** (auto-discovered, but can add explicitly):
```csharp
public DbSet<MyEntity> MyEntities { get; set; }
```

3. **Create repository interface** in `Domain/Interfaces/`:
```csharp
public interface IMyEntityRepository
{
    Task<MyEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<MyEntity> AddAsync(MyEntity entity, CancellationToken cancellationToken = default);
}
```

4. **Implement repository** in `Infrastructure/Repositories/`:
```csharp
public class MyEntityRepository : IMyEntityRepository
{
    private readonly AnswerMeDbContext _context;
    // Implementation using _context.MyEntities
}
```

5. **Create service interface** in `Application/Interfaces/`:
```csharp
public interface IMyEntityService
{
    Task<MyEntityDto> CreateAsync(CreateMyEntityDto dto, CancellationToken cancellationToken = default);
}
```

6. **Implement service** in `Application/Services/`

7. **Create controller** in `API/Controllers/`:
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MyEntitiesController : ControllerBase
{
    // Inject service, implement endpoints
}
```

8. **Register services** in `Program.cs`:
```csharp
builder.Services.AddScoped<IMyEntityRepository, MyEntityRepository>();
builder.Services.AddScoped<IMyEntityService, MyEntityService>();
```

9. **Create migration**:
```bash
dotnet ef migrations add AddMyEntity --project AnswerMe.Infrastructure --startup-project AnswerMe.API
```

### Error Handling

**Global exception filter** registered in `Program.cs`:
```csharp
options.Filters.Add<AnswerMe.API.Filters.GlobalExceptionFilter>();
```

**Service layer exceptions:**
- `InvalidOperationException` - Business rule violations
- `UnauthorizedAccessException` - Permission issues

**Controller error responses:**
```csharp
if (!ModelState.IsValid)
{
    return BadRequest(ModelState);
}

try
{
    var result = await _service.SomeMethodAsync(dto);
    return Ok(result);
}
catch (InvalidOperationException ex)
{
    return BadRequest(new { message = ex.Message });
}
catch (Exception ex)
{
    _logger.LogError(ex, "操作失败");
    return StatusCode(500, new { message = "服务器内部错误，请稍后重试" });
}
```

### Logging

**Serilog** configured in `Program.cs`:
- Console output
- File output: `logs/answerme-.log` (7-day retention)
- Structured logging with properties

**Usage in services:**
```csharp
_logger.LogInformation("用户 {UserId} 执行操作", userId);
_logger.LogError(ex, "操作失败: {Data}", data);
```

## Testing

**Test project**: `backend/AnswerMe.UnitTests/`

**Run tests:**
```bash
cd backend
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~TestMethodName"
```

## Environment Variables

**Required:**
- `JWT_SECRET` - At least 32 characters (JWT signing)
- `ENCRYPTION_KEY` - For API key encryption in production

**Optional:**
- `DB_TYPE` - "Sqlite" (default) or "PostgreSQL"
- `ConnectionStrings__DefaultConnection` - Database connection string
- `JWT__Issuer` - JWT issuer (default: "AnswerMe")
- `JWT__Audience` - JWT audience (default: "AnswerMeUsers")
- `JWT__ExpiryDays` - Token expiry in days (default: 30)
- `ALLOWED_ORIGINS` - CORS origins (default: "http://localhost:3000,http://localhost:5173")

## Migration Tasks

The project uses OpenSpec workflow for change management. Current change: `ai-questionbank-mvp`

**To continue implementation:**
```bash
/opsx:apply ai-questionbank-mvp
```

**Tasks file location:** `openspec/changes/ai-questionbank-mvp/tasks.md`

## Known Issues and Limitations

1. **Async task storage** - Currently uses in-memory `Dictionary<>` for AI generation tasks. Production should use Redis or database.
2. **AI Provider API Key injection** - `OpenAIProvider.GetApiKey()` returns placeholder. Needs proper integration with `DataSource.Config` decryption.
3. **Single AI Provider** - Only OpenAI implemented. Qwen/智谱GLM/Minimax providers pending.
4. **Testing** - Unit tests minimal. Need comprehensive test coverage.

## Frontend Notes

**Frontend location**: `frontend/AnswerMe.Frontend/`

**Vue 3 Composition API** with TypeScript.

**Pinia stores**: `frontend/AnswerMe.Frontend/src/stores/`

**Router**: `frontend/AnswerMe.Frontend/src/router/`

**Axios base URL**: Points to `http://localhost:5000/api` (backend API)

**CORS**: Backend allows `http://localhost:5173` by default (Vite dev server)

## Deployment

**Production deployment via Docker:**

1. Copy `.env.example` to `.env` and configure
2. Set strong `JWT_SECRET` and `ENCRYPTION_KEY`
3. Run: `docker-compose up -d`
4. Access frontend at `http://localhost:3000`
5. API at `http://localhost:5000`

**Health check endpoint:** `GET /health`

## Development Workflow

1. Make changes to domain entities first
2. Create migration: `dotnet ef migrations add ...`
3. Update repository interfaces if needed
4. Implement repository changes
5. Update/create services for business logic
6. Create/update DTOs
7. Add/update controller endpoints
8. Test manually or via Postman
9. Run tests: `dotnet test`
10. Commit with conventional commit message

## Chinese Language Support

The project uses Chinese for user-facing content. All DTOs, entities, and API messages support Chinese text. Database stores Chinese text correctly with SQLite and PostgreSQL.
