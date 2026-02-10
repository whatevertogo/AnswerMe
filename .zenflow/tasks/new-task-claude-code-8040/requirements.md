# Product Requirements Document (PRD)
## AI Question Generation System Refactor

**Version**: 1.0
**Date**: 2025-02-09
**Status**: Draft

---

## Executive Summary

The AI Question Generation System in AnswerMe has accumulated significant technical debt. This document outlines requirements for a comprehensive refactoring to improve maintainability, type safety, and support for additional question types (notably multiple-choice questions with multiple correct answers).

### Problem Statement

Current issues identified:

1. **Type System Fragmentation**: Question types exist as strings across 4+ layers with inconsistent values
   - Backend uses: `"choice"`, `"fill_blank"`, `"essay"`
   - Frontend uses: `"choice"`, `"multiple-choice"`, `"true-false"`, `"short-answer"`
   - AI prompts use Chinese: `"选择题"`, `"填空题"`

2. **Data Model Limitation**: `CorrectAnswer` is a single string, cannot store multiple correct answers for multiple-choice questions

3. **90% Code Duplication**: 5 AI providers (OpenAI, Qwen, Zhipu, MiniMax, CustomApi) each contain ~200 lines of nearly identical code

4. **Broken Retry Logic**: String literal `"RATE_LIMIT_EXCEEDED"` compared with HTTP status code `"429"`

5. **Unused Validator**: `AIResponseValidator` exists but providers bypass it with their own parsing

6. **In-Memory Async Tasks**: Tasks lost on application restart, no persistence

### Success Metrics

- Code duplication reduced by 80%+ (measured by lines of code)
- Zero type mismatches between frontend and backend
- All questions validated before database save
- Async tasks survive application restart
- Multiple-choice questions support 2-3 correct answers

---

## Stakeholders

| Role | Needs |
|------|-------|
| End Users | Reliable question generation, support for multiple-choice questions |
| Frontend Developers | Type-safe APIs, consistent data structures |
| Backend Developers | Maintainable, testable code following SOLID principles |
| DevOps | Deployable with feature flags for gradual rollout |

---

## Goals

### Primary Goals

1. **Unified Type System**: Single `QuestionType` enum used across all layers
2. **Type-Safe Data Model**: Polymorphic `QuestionData` hierarchy supporting all question types
3. **Code Quality**: Eliminate duplication, follow SOLID principles
4. **Multiple-Choice Support**: Support 2-3 correct answers per question
5. **Robust Parsing**: Handle various AI response formats gracefully
6. **Validation**: Enforce quality standards before database save
7. **Persistence**: Async tasks survive application restarts

### Non-Goals

- AI model optimization (use generic best practices)
- UI redesign (only adjust data structures)
- Advanced scoring (partial credit for multiple-choice deferred)
- Real-time collaboration
- Detailed analytics (only basic logging)

---

## Functional Requirements

### FR1: Question Type Enumeration

**REQ-1.1**: Create a centralized `QuestionType` enum with values:
- `SingleChoice` - Single correct answer
- `MultipleChoice` - Multiple correct answers
- `TrueFalse` - Boolean answer
- `FillBlank` - Fill in blank
- `ShortAnswer` - Free text answer

**REQ-1.2**: Enum must have extension methods:
- `DisplayName()` - Returns localized display name (Chinese/English)
- `ToAiPrompt()` - Returns format expected by AI

**REQ-1.3**: Enum must be shared across:
- Backend C# code
- Frontend TypeScript code
- Database enum constraint (PostgreSQL)

### FR2: Question Data Model

**REQ-2.1**: Create polymorphic `QuestionData` hierarchy:
```
QuestionData (abstract)
├── ChoiceQuestionData
│   ├── Options: List<string>
│   └── CorrectAnswers: List<string>
├── BooleanQuestionData
│   └── CorrectAnswer: bool
├── FillBlankQuestionData
│   └── AcceptableAnswers: List<string>
└── ShortAnswerQuestionData
    └── ReferenceAnswer: string
```

**REQ-2.2**: Store as JSON in database `question_data` column

**REQ-2.3**: Backward compatibility: Keep existing `Options` and `CorrectAnswer` columns marked `[Obsolete]`

### FR3: AI Provider Refactoring

**REQ-3.1**: Create `BaseAIProvider` abstract class implementing `IAIProvider`

**REQ-3.2**: Implement template method pattern with shared workflow:
1. Build prompt
2. Build HTTP request
3. Send with retry
4. Parse response

**REQ-3.3**: Each concrete provider only implements:
- Default model name
- Default endpoint
- API key validation

**REQ-3.4**: Fix retry logic to check HTTP status codes (429, 503, 504)

### FR4: Response Parsing

**REQ-4.1**: Create `SmartResponseParser` with multiple strategies:
1. Direct JSON array: `[...]`
2. Questions object: `{"questions": [...]}`
3. Markdown code block: ` ```json ... ``` `
4. JSON in text: Extract from mixed content
5. Lax JSON: Handle trailing commas, single quotes

**REQ-4.2**: Strategies tried in order; first success returns

**REQ-4.3**: Log successful strategy for monitoring

### FR5: Validation

**REQ-5.1**: Create `QuestionValidationChain` routing to type-specific validators

**REQ-5.2**: Common validation (all types):
- Question text not empty
- Difficulty is valid value
- Required fields present

**REQ-5.3**: Type-specific validation:
- SingleChoice: Exactly 1 correct answer, answer in options
- MultipleChoice: 2-3 correct answers, all in options
- TrueFalse: Boolean answer, no options field
- FillBlank: At least 1 acceptable answer
- ShortAnswer: Reference answer exists

### FR6: Orchestrator

**REQ-6.1**: Create `QuestionGenerationOrchestrator` coordinating:
1. Prompt building
2. AI generation (with retry)
3. Response parsing
4. Validation
5. Database save

**REQ-6.2**: Return partial success details (valid/invalid counts)

**REQ-6.3**: Log quality metrics (validation pass rate)

### FR7: Async Task Persistence

**REQ-7.1**: Create `AITask` entity with fields:
- `Id` (string, GUID)
- `UserId`
- `Status` (pending, processing, completed, failed, partial_success)
- `RequestJson`
- `ResultJson`
- `ErrorMessage`
- `GeneratedCount`
- `TotalCount`
- `CreatedAt`
- `CompletedAt`

**REQ-7.2**: Implement task resumption after restart

**REQ-7.3**: Implement cleanup job for tasks older than 30 days

---

## Non-Functional Requirements

### NFR1: Performance

- Generate 10 questions in <30 seconds
- Support 100 concurrent generation requests
- Database queries include proper indexes

### NFR2: Compatibility

- Support SQLite (development) and PostgreSQL (production)
- No breaking API changes for existing clients
- Feature flag for gradual rollout

### NFR3: Code Quality

- >90% test coverage for new code
- All code follows SOLID principles
- No code duplication >80% reduction

### NFR4: Security

- API keys remain encrypted with Data Protection API
- No API keys in logs or responses
- Input validation on all endpoints

### NFR5: Observability

- Log all generation attempts
- Track success/failure rates
- Monitor retry occurrences

---

## Data Model Changes

### Existing Question Entity

```csharp
public class Question {
    public int Id { get; set; }
    public string QuestionText { get; set; }
    public string QuestionType { get; set; }  // TO BE: QuestionType enum
    public string? Options { get; set; }       // JSON array
    public string CorrectAnswer { get; set; }  // TO BE: in QuestionData
    public string? Explanation { get; set; }
    public string Difficulty { get; set; }
    // ...
}
```

### New Question Entity

```csharp
public class Question {
    public int Id { get; set; }
    public string QuestionText { get; set; }
    public QuestionType QuestionType { get; set; }  // NEW: Enum

    [Column(TypeName = "json")]
    public string QuestionDataJson { get; set; }  // NEW: JSON column

    [NotMapped]
    public QuestionData? Data {
        get => Deserialize<QuestionData>(QuestionDataJson);
        set => QuestionDataJson = Serialize(value);
    }

    [Obsolete] public string? Options { get; set; }      // KEEP for migration
    [Obsolete] public string CorrectAnswer { get; set; } // KEEP for migration
    public string? Explanation { get; set; }
    public string Difficulty { get; set; }
    // ...
}
```

### New AITask Entity

```csharp
public class AITask {
    public string Id { get; set; }  // GUID
    public int UserId { get; set; }
    public AITaskStatus Status { get; set; }
    public string RequestJson { get; set; }
    public string? ResultJson { get; set; }
    public string? ErrorMessage { get; set; }
    public int GeneratedCount { get; set; }
    public int TotalCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
```

---

## API Changes

### Existing Endpoints (Unchanged)

- `POST /api/questions/generate` - Synchronous generation (≤20 questions)
- `POST /api/questions/generate-async` - Async generation (>20 questions)
- `GET /api/questions/progress/{taskId}` - Get async task progress

### Request/Response Changes

**Before:**
```json
{
  "questionTypes": ["choice", "multiple-choice"]
}
```

**After:**
```json
{
  "questionTypes": ["SingleChoice", "MultipleChoice"]
}
```

**Response includes:**
```json
{
  "success": true,
  "questions": [...],
  "partialSuccessCount": 2  // NEW: count of failed questions
}
```

---

## Migration Strategy

### Phase 1: Foundation (Week 1)

1. Create database migrations
2. Backup existing questions table
3. Add new columns (keep old ones)
4. Create QuestionType enum
5. Create QuestionData hierarchy

### Phase 2: Core Refactoring (Week 2-3)

1. Create BaseAIProvider
2. Refactor all providers to inherit from it
3. Create SmartResponseParser
4. Create validation chain
5. Create orchestrator

### Phase 3: Persistence (Week 4)

1. Create AITask entity and repository
2. Update async task handling
3. Implement task resumption
4. Add cleanup job

### Phase 4: Frontend Updates (Week 5)

1. Update TypeScript types
2. Update components for new types
3. Update quiz interface for multiple-choice
4. Update grading logic

### Phase 5: Data Migration & Rollout (Week 6)

1. Run data migration script
2. Deploy to staging
3. Enable feature flag for 10% of users
4. Monitor metrics for 48 hours
5. Gradual rollout to 100%

---

## Acceptance Criteria

A feature is considered complete when:

1. All unit tests pass with >90% coverage
2. All integration tests pass
3. Code duplication reduced by >80%
4. Multiple-choice questions work end-to-end
5. Async tasks persist across restarts
6. Feature flag enables gradual rollout
7. No breaking changes for existing clients
8. Documentation updated

---

## Open Questions

| Question | Impact | Decision |
|----------|--------|----------|
| Should we use database enum or C# enum? | Medium | Use C# enum, database check constraint |
| How long to keep obsolete columns? | Low | 30 days after migration verified |
| Should we add rate limiting at API level? | Medium | Use existing rate limiting middleware |

---

## Assumptions

1. Existing AI API keys and configurations remain valid
2. Database migration can be done without downtime
3. Frontend and backend can be deployed independently
4. Users have acceptable tolerance for brief migration period
5. AI providers' response formats remain stable enough for parsing strategies

---

## Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Data migration fails | Low | High | Backup table, test migration on copy |
| AI format changes break parsers | Medium | Medium | Multiple strategies, logging |
| Performance regression | Low | Medium | Performance testing before rollout |
| Feature flag issues | Low | High | Staging testing, gradual rollout |

---

## Dependencies

- .NET 10 Web API
- EF Core 10
- Vue 3 + TypeScript
- SQLite (dev) / PostgreSQL (prod)
- Existing AI provider APIs
