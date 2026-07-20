# GastronomIQ System Architecture

**Version:** 1.0  
**Last Updated:** 2026-07-20  
**Owner:** Culinaria Professionali LLP  
**Status:** Foundation - Ready for Implementation

---

## Table of Contents

1. [System Overview](#system-overview)
2. [Architecture Diagram](#architecture-diagram)
3. [Technology Stack](#technology-stack)
4. [Module Boundaries](#module-boundaries)
5. [Authentication Architecture](#authentication-architecture)
6. [Multi-Tenant Organization Model](#multi-tenant-organization-model)
7. [API Conventions](#api-conventions)
8. [Error Handling Strategy](#error-handling-strategy)
9. [Logging & Audit Architecture](#logging--audit-architecture)
10. [Data Flow](#data-flow)
11. [Security Model](#security-model)
12. [Scalability Considerations](#scalability-considerations)

---

## System Overview

GastronomIQ is a **multi-tenant, cloud-native recipe development platform** designed for culinary enterprises. It enables chefs and operations teams to manage recipes, ingredients, costs, inventory, and nutritional information with collaboration and reporting capabilities.

### Core Principles

- **Multi-Tenancy:** Isolated organizations with shared infrastructure
- **API-First:** Backend-agnostic, enabling mobile and web clients
- **Scalability:** Horizontal scaling via containerization
- **Security:** JWT authentication, RBAC, audit logging
- **Reliability:** Event-driven architecture with transactional guarantees
- **Compliance:** GDPR-ready, PCI-DSS considerations

---

## Architecture Diagram

### High-Level System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        PRESENTATION LAYER                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│    ┌─────────────┐         ┌──────────────┐   ┌─────────────┐   │
│    │   Flutter   │         │  Web Client  │   │  Admin UI   │   │
│    │   Mobile    │         │  (React/Vue) │   │  (Internal) │   │
│    └──────┬──────┘         └──────┬───────┘   └──────┬──────┘   │
│           │                        │                   │           │
└───────────┼────────────────────────┼───────────────────┼───────────┘
            │                        │                   │
            └────────────┬───────────┴───────────────────┘
                         │
            ┌────────────▼───────────┐
            │   API Gateway / LB     │
            │  (Rate Limiting, CORS) │
            └────────────┬───────────┘
                         │
┌────────────────────────▼────────────────────────────────────────┐
│              APPLICATION LAYER (ASP.NET Core)                    │
├────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────────┐ │
│  │  Auth Service   │  │  Organization   │  │  Ingredient      │ │
│  │  - Login        │  │  Service        │  │  Service         │ │
│  │  - Token Mgmt   │  │  - Teams        │  │  - CRUD          │ │
│  │  - 2FA          │  │  - Roles        │  │  - Categories    │ │
│  │  - Permissions  │  │  - Settings     │  │  - Nutrition     │ │
│  └─────────────────┘  └─────────────────┘  └──────────────────┘ │
│                                                                   │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────────┐ │
│  │  Recipe Service │  │  Inventory      │  │  Report Service  │ │
│  │  - CRUD         │  │  Service        │  │  - Analytics     │ │
│  │  - Steps        │  │  - Stock Mgmt   │  │  - Costs         │ │
│  │  - Versions     │  │  - Alerts       │  │  - Trends        │ │
│  │  - Publishing   │  │  - Valuation    │  │  - Export        │ │
│  └─────────────────┘  └─────────────────┘  └──────────────────┘ │
│                                                                   │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────────┐ │
│  │  Cost Engine    │  │  Notification   │  │  AI Assistant    │ │
│  │  - Calculation  │  │  Service        │  │  - Suggestions   │ │
│  │  - Comparison   │  │  - Email        │  │  - Optimization  │ │
│  │  - History      │  │  - WebSocket    │  │  - Substitutions │ │
│  │  - Profit       │  │  - In-App       │  │  (External API)  │ │
│  └─────────────────┘  └─────────────────┘  └──────────────────┘ │
│                                                                   │
└────────────────────────────────────────────────────────────────┘
                         │
        ┌────────────────┼────────────────┐
        │                │                │
┌───────▼──────┐  ┌──────▼───────┐  ┌───▼────────────┐
│ Message Bus  │  │ Cache Layer  │  │ Email Service  │
│ (RabbitMQ /  │  │ (Redis)      │  │ (SendGrid)     │
│  Kafka)      │  │              │  │                │
└───────┬──────┘  └──────┬───────┘  └────────────────┘
        │                │
┌───────▼────────────────▼─────────────────────────┐
│            DATA ACCESS LAYER                      │
├────────────────────────────────────────────────┤
│                                                   │
│  ┌──────────────────────────────────────────┐   │
│  │  Entity Framework Core / ORM              │   │
│  │  - Models                                 │   │
│  │  - Repositories                           │   │
│  │  - Query Optimization                     │   │
│  └──────────────────────────────────────────┘   │
│                                                   │
└───────────────────┬─────────────────────────────┘
                    │
┌───────────────────▼─────────────────────────────┐
│          PERSISTENCE LAYER                       │
├────────────────────────────────────────────────┤
│                                                   │
│  ┌──────────────────────────────────────────┐   │
│  │  PostgreSQL (Primary)                     │   │
│  │  - Multi-tenant schema                    │   │
│  │  - ACID transactions                      │   │
│  │  - Full-text search                       │   │
│  │  - Audit tables                           │   │
│  └──────────────────────────────────────────┘   │
│                                                   │
│  ┌──────────────────────────────────────────┐   │
│  │  Object Storage (S3/Blob)                 │   │
│  │  - Recipe images                          │   │
│  │  - Nutrition data exports                 │   │
│  └──────────────────────────────────────────┘   │
│                                                   │
└────────────────────────────────────────────────┘

┌────────────────────────────────────────────────┐
│         EXTERNAL INTEGRATIONS                   │
├────────────────────────────────────────────────┤
│                                                  │
│  • Nutrition API (USDA FoodData Central)       │
│  • Payment Processing (Stripe / Square)        │
│  • Email Service (SendGrid / AWS SES)          │
│  • AI/ML Platform (OpenAI / Custom Models)     │
│  • Analytics (Mixpanel / Segment)              │
│                                                  │
└────────────────────────────────────────────────┘
```

---

## Technology Stack

### Backend
- **Framework:** ASP.NET Core 7+
- **Language:** C#
- **Database:** PostgreSQL 14+
- **ORM:** Entity Framework Core
- **Message Queue:** RabbitMQ / Azure Service Bus
- **Cache:** Redis
- **API Documentation:** OpenAPI/Swagger

### Frontend
- **Mobile:** Flutter (iOS & Android)
- **Web:** React 18+ or Vue 3+
- **State Management:** Redux / Pinia
- **HTTP Client:** Axios / native HTTP
- **UI Framework:** Material Design / Tailwind CSS

### Infrastructure
- **Containerization:** Docker
- **Orchestration:** Kubernetes (Production) / Docker Compose (Dev)
- **Cloud Provider:** Azure / AWS / GCP
- **CI/CD:** GitHub Actions / Azure Pipelines
- **Monitoring:** Application Insights / Prometheus / ELK Stack

### Security
- **Authentication:** JWT (JSON Web Tokens)
- **Password Hashing:** bcrypt / PBKDF2
- **Encryption:** TLS 1.3, AES-256 at rest
- **API Security:** Helmet, CORS, Rate Limiting

---

## Module Boundaries

### Service-Oriented Architecture

GastronomIQ uses a **modular monolith** architecture with clear service boundaries, enabling future migration to microservices:

```
┌─────────────────────────────────────────────────────────┐
│                  API Gateway Layer                       │
│            (HTTP routing, authentication)                │
└────────┬──────────────────────────────────────────┬─────┘
         │                                          │
    ┌────▼──────────────┐              ┌───────────▼──────┐
    │  Authentication   │              │  Authorization   │
    │  Middleware       │              │  Middleware      │
    │  - JWT Validation │              │  - RBAC Check    │
    │  - Token Refresh  │              │  - Tenant Scope  │
    └────┬──────────────┘              └───────────┬──────┘
         │                                         │
    ┌────▼─────────────────────────────────────────▼─────┐
    │                                                      │
    │  ┌──────────────────────────────────────────────┐   │
    │  │      AUTH DOMAIN                             │   │
    │  │  Responsible for: User identity & tokens     │   │
    │  │  Exports: IAuthService                       │   │
    │  │  - RegisterAsync(user, password)             │   │
    │  │  - LoginAsync(email, password)               │   │
    │  │  - RefreshTokenAsync(token)                  │   │
    │  │  - ValidateMFAAsync(userId, code)            │   │
    │  │  - RevokeTokenAsync(token)                   │   │
    │  └──────────────────────────────────────────────┘   │
    │                                                      │
    │  ┌──────────────────────────────────────────────┐   │
    │  │      ORGANIZATION DOMAIN                     │   │
    │  │  Responsible for: Teams, roles, access       │   │
    │  │  Exports: IOrganizationService               │   │
    │  │  - CreateOrganizationAsync(details)          │   │
    │  │  - InviteUserAsync(email, role)              │   │
    │  │  - AssignRoleAsync(userId, role)             │   │
    │  │  - UpdateSettingsAsync(settings)             │   │
    │  │  - ListTeamMembersAsync()                    │   │
    │  └──────────────────────────────────────────────┘   │
    │                                                      │
    │  ┌──────────────────────────────────────────────┐   │
    │  │      INGREDIENT DOMAIN                       │   │
    │  │  Responsible for: Ingredient master data     │   │
    │  │  Exports: IIngredientService                 │   │
    │  │  - CreateIngredientAsync(details)            │   │
    │  │  - UpdateIngredientAsync(id, details)        │   │
    │  │  - GetIngredientAsync(id)                    │   │
    │  │  - ListIngredientsAsync(filters)             │   │
    │  │  - DeleteIngredientAsync(id)                 │   │
    │  │  - GetNutritionDataAsync(ingredientId)       │   │
    │  └──────────────────────────────────────────────┘   │
    │                                                      │
    │  ┌──────────────────────────────────────────────┐   │
    │  │      RECIPE DOMAIN                           │   │
    │  │  Responsible for: Recipe definitions         │   │
    │  │  Exports: IRecipeService                     │   │
    │  │  - CreateRecipeAsync(details)                │   │
    │  │  - UpdateRecipeAsync(id, details)            │   │
    │  │  - GetRecipeAsync(id)                        │   │
    │  │  - ListRecipesAsync(filters)                 │   │
    │  │  - PublishRecipeAsync(id)                    │   │
    │  │  - CalculateNutritionAsync(recipeId)         │   │
    │  │  - AddIngredientAsync(recipeId, ingredientId)│   │
    │  │  - AddStepAsync(recipeId, step)              │   │
    │  └──────────────────────────────────────────────┘   │
    │                                                      │
    │  ┌──────────────────────────────────────────────┐   │
    │  │      COST DOMAIN                             │   │
    │  │  Responsible for: Recipe cost calculations   │   │
    │  │  Exports: ICostService                       │   │
    │  │  - CalculateRecipeCostAsync(recipeId)        │   │
    │  │  - CalculateServingCostAsync(recipeId)       │   │
    │  │  - GetCostHistoryAsync(recipeId)             │   │
    │  │  - CompareRecipesCostAsync(recipeIds)        │   │
    │  │  - CalculateProfitMarginAsync(recipeId)      │   │
    │  └──────────────────────────────────────────────┘   │
    │                                                      │
    │  ┌──────────────────────────────────────────────┐   │
    │  │      INVENTORY DOMAIN                        │   │
    │  │  Responsible for: Stock & consumption        │   │
    │  │  Exports: IInventoryService                  │   │
    │  │  - AddStockAsync(ingredientId, qty)          │   │
    │  │  - ConsumeStockAsync(ingredientId, qty)      │   │
    │  │  - GetInventoryAsync(ingredientId)           │   │
    │  │  - GetLowStockItemsAsync()                   │   │
    │  │  - CalculateValuationAsync()                 │   │
    │  └──────────────────────────────────────────────┘   │
    │                                                      │
    │  ┌──────────────────────────────────────────────┐   │
    │  │      REPORTING DOMAIN                        │   │
    │  │  Responsible for: Analytics & exports        │   │
    │  │  Exports: IReportingService                  │   │
    │  │  - GenerateCostReportAsync(dateRange)        │   │
    │  │  - GenerateInventoryReportAsync()            │   │
    │  │  - GetRecipeAnalyticsAsync(recipeId)         │   │
    │  │  - ExportReportAsync(reportId, format)       │   │
    │  │  - GetTrendsAsync(timeframe)                 │   │
    │  └──────────────────────────────────────────────┘   │
    │                                                      │
    │  ┌──────────────────────────────────────────────┐   │
    │  │      NOTIFICATION DOMAIN                     │   │
    │  │  Responsible for: User notifications         │   │
    │  │  Exports: INotificationService               │   │
    │  │  - SendEmailAsync(userId, email)             │   │
    │  │  - SendInAppNotificationAsync(userId, msg)   │   │
    │  │  - SendSmsAsync(userId, message) [Future]    │   │
    │  │  - PublishEventAsync(eventType, data)        │   │
    │  └──────────────────────────────────────────────┘   │
    │                                                      │
    │  ┌──────────────────────────────────────────────┐   │
    │  │      AI ASSISTANT DOMAIN [Future]            │   │
    │  │  Responsible for: AI-powered recommendations │   │
    │  │  Exports: IAssistantService                  │   │
    │  │  - GetRecipeSuggestionsAsync(context)        │   │
    │  │  - OptimizeCostAsync(recipeId)               │   │
    │  │  - GetSubstitutionsAsync(ingredientId)       │   │
    │  │  - GetNutritionRecommendationsAsync()        │   │
    │  └──────────────────────────────────────────────┘   │
    │                                                      │
    └──────────────────────────────────────────────────────┘
```

### Domain Responsibilities

| Domain | Ownership | Key Entities | Dependencies |
|--------|-----------|--------------|--------------|
| **Auth** | User identity & tokens | User, Role, Token | None (foundational) |
| **Organization** | Teams & access control | Organization, Team, TeamMember, Permission | Auth |
| **Ingredient** | Master data | Ingredient, Category, Allergen, NutritionData | Organization |
| **Recipe** | Recipe definitions | Recipe, RecipeVersion, RecipeStep, RecipeIngredient | Organization, Ingredient |
| **Cost** | Cost calculations | RecipeCost, CostHistory | Recipe, Ingredient |
| **Inventory** | Stock management | InventoryItem, StockMovement | Organization, Ingredient |
| **Reporting** | Analytics & reports | Report, ReportConfig | Recipe, Inventory, Cost |
| **Notification** | User messaging | Notification, NotificationQueue | Organization |
| **AI Assistant** | Recommendations | Suggestion, OptimizationResult | All domains (read-only) |

---

## Authentication Architecture

### JWT-Based Authentication Flow

```
┌──────────────┐
│   Client     │
└──────┬───────┘
       │
       │ 1. POST /auth/login
       │    { email, password }
       │
       ▼
┌──────────────────────────────────┐
│  ASP.NET Core Authentication     │
│  Controller                      │
└──────┬───────────────────────────┘
       │
       │ 2. Validate credentials
       │    bcrypt compare
       │
       ▼
┌──────────────────────────────────┐
│  User Service                    │
│  - Find user by email            │
│  - Validate password hash        │
└──────┬───────────────────────────┘
       │
       │ 3. Generate JWT
       │
       ▼
┌──────────────────────────────────────────┐
│  JWT Payload                             │
│  {                                       │
│    sub: userId,                          │
│    org: organizationId,                  │
│    email: user.email,                    │
│    roles: [role1, role2],                │
│    permissions: [perm1, perm2],          │
│    iat: timestamp,                       │
│    exp: timestamp + 7d                   │
│  }                                       │
└──────┬───────────────────────────────────┘
       │
       │ 4. Return tokens
       │    { accessToken, refreshToken }
       │
       ▼
┌──────────────┐
│   Client     │
│ Stores JWT   │
└──────┬───────┘
       │
       │ 5. Subsequent requests
       │    Header: Authorization: Bearer <JWT>
       │
       ▼
┌──────────────────────────────────┐
│  Authentication Middleware       │
│  - Parse JWT                     │
│  - Validate signature            │
│  - Check expiration              │
│  - Extract claims                │
└──────┬───────────────────────────┘
       │
       │ 6. Valid? Add to HttpContext
       │
       ▼
┌──────────────────────────────────┐
│  Authorization Middleware        │
│  - Check RBAC                    │
│  - Verify tenant scoping         │
│  - Check resource permissions    │
└──────┬───────────────────────────┘
       │
       │ 7. Authorized?
       │
       ▼
┌──────────────────────────────────┐
│  Route Handler / Controller      │
│  Process request                 │
└──────────────────────────────────┘
```

### Token Types

**Access Token (Short-lived, ~15 minutes)**
- Contains user identity and permissions
- Sent with every request
- Compact for frequent transmission

**Refresh Token (Long-lived, ~7 days)**
- Stored securely on client
- Used to obtain new access tokens
- Revocable server-side
- Never sent in Authorization header

### Multi-Factor Authentication (2FA)

```
Optional MFA Flow:
1. User login → valid credentials
2. System generates 6-digit code → sends via email/SMS
3. User enters code
4. Code validation against time-based TOTP
5. If valid → issue JWT tokens
6. If invalid → reject and log attempt
```

---

## Multi-Tenant Organization Model

### Data Isolation Strategy

```
┌─────────────────────────────────────┐
│      Database (Single Instance)      │
│  PostgreSQL with Row-Level Security  │
└────────────────────┬────────────────┘
                     │
         ┌───────────┼───────────┐
         │           │           │
    ┌────▼────┐ ┌────▼────┐ ┌────▼────┐
    │ Org A   │ │ Org B   │ │ Org C   │
    │ Data    │ │ Data    │ │ Data    │
    │ (RLS)   │ │ (RLS)   │ │ (RLS)   │
    └─────────┘ └─────────┘ └─────────┘
```

### Tenant Scoping

Every request includes tenant context:

```csharp
// ClaimsPrincipal extracted from JWT
var organizationId = User.FindFirst("org")?.Value;
var userId = User.FindFirst("sub")?.Value;
var roles = User.FindAll(ClaimTypes.Role);

// Tenant Context Object
public class TenantContext
{
    public string OrganizationId { get; set; }      // UUID
    public string UserId { get; set; }               // UUID
    public List<string> Roles { get; set; }          // user, editor, admin, owner
    public List<string> Permissions { get; set; }    // granular permissions
}
```

### Row-Level Security (RLS)

PostgreSQL policies enforce tenant isolation at the database level:

```sql
-- Example RLS policy
CREATE POLICY recipes_isolation ON recipes
  USING (organization_id = current_setting('app.organization_id')::uuid);

-- Set tenant context per request
SET app.organization_id = '550e8400-e29b-41d4-a716-446655440000';
```

### Organizational Hierarchy

```
┌─────────────────────────────────┐
│     Organization                │
│  (Restaurant / Company)         │
│  - name, industry, location     │
│  - billing_plan, features       │
└────────────────────┬────────────┘
                     │
        ┌────────────┼────────────┐
        │            │            │
   ┌────▼────┐  ┌────▼────┐  ┌────▼────┐
   │ Team    │  │ Team    │  │ Team    │
   │ Kitchen │  │ Admin   │  │ Cost    │
   └────┬────┘  └────┬────┘  └────┬────┘
        │            │            │
    ┌───▼────┐   ┌───▼────┐   ┌───▼────┐
    │ Member │   │ Member │   │ Member │
    │ 1      │   │ 2      │   │ 3      │
    └────────┘   └────────┘   └────────┘

Roles & Permissions:
- Owner: Full control, billing
- Admin: Users, settings, audit logs
- Editor: Create/modify recipes, ingredients
- Viewer: Read-only access
```

---

## API Conventions

### Base URL & Versioning

```
https://api.gastronomiq.com/api/v1/
```

### Request/Response Format

**Headers:**
```
Content-Type: application/json
Authorization: Bearer <JWT>
X-Organization-Id: <uuid> [Optional - derived from JWT]
X-Request-Id: <uuid> [Tracing]
```

**Request Body (POST/PUT):**
```json
{
  "name": "string",
  "description": "string",
  "metadata": {}
}
```

**Success Response (200, 201, 204):**
```json
{
  "statusCode": 200,
  "message": "Request successful",
  "data": {
    "id": "uuid",
    "name": "string",
    "createdAt": "2026-07-20T07:30:00Z",
    "updatedAt": "2026-07-20T07:30:00Z"
  },
  "timestamp": "2026-07-20T07:30:00Z"
}
```

**Paginated Response (200):**
```json
{
  "statusCode": 200,
  "message": "Request successful",
  "data": [
    { "id": "uuid", "name": "string" }
  ],
  "pagination": {
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 145,
    "totalPages": 8,
    "hasNextPage": true,
    "hasPreviousPage": false
  },
  "timestamp": "2026-07-20T07:30:00Z"
}
```

### HTTP Methods & Semantics

| Method | Purpose | Status Codes |
|--------|---------|--------------|
| GET | Retrieve resource(s) | 200, 404, 401 |
| POST | Create new resource | 201, 400, 409 |
| PUT | Replace entire resource | 200, 400, 404 |
| PATCH | Partial update | 200, 400, 404 |
| DELETE | Remove resource | 204, 404, 409 |

### Pagination

```
GET /api/v1/recipes?pageNumber=1&pageSize=20&sortBy=createdAt&sortOrder=desc
```

### Filtering

```
GET /api/v1/recipes?search=pasta&cuisine=italian&difficulty=medium
```

### Partial Responses

```
GET /api/v1/recipes?fields=id,name,difficulty
```

---

## Error Handling Strategy

### Standardized Error Response

```json
{
  "statusCode": 400,
  "errorCode": "VALIDATION_ERROR",
  "message": "One or more validation errors occurred",
  "errors": [
    {
      "field": "name",
      "message": "Name is required",
      "code": "REQUIRED_FIELD"
    },
    {
      "field": "difficulty",
      "message": "Invalid difficulty value",
      "code": "INVALID_ENUM"
    }
  ],
  "traceId": "0HN1GH7F8ASDF",
  "timestamp": "2026-07-20T07:30:00Z"
}
```

### HTTP Status Codes & Error Categories

| Status | Code | Meaning | Example |
|--------|------|---------|---------|
| 400 | BAD_REQUEST | Invalid input | Missing required field |
| 401 | UNAUTHORIZED | No valid auth | Missing JWT |
| 403 | FORBIDDEN | No permission | Insufficient role |
| 404 | NOT_FOUND | Resource missing | Recipe ID not found |
| 409 | CONFLICT | State violation | Duplicate email |
| 422 | UNPROCESSABLE | Validation failed | Invalid data format |
| 429 | RATE_LIMITED | Too many requests | 100 requests/min |
| 500 | INTERNAL_ERROR | Server error | Unexpected exception |
| 503 | SERVICE_UNAVAILABLE | Temporary issue | Database down |

### Error Code Taxonomy

```
VALIDATION_ERROR - Input validation failed
AUTHENTICATION_ERROR - Login/token issues
AUTHORIZATION_ERROR - Permission denied
NOT_FOUND_ERROR - Resource doesn't exist
CONFLICT_ERROR - Business rule violation
RATE_LIMIT_ERROR - Too many requests
EXTERNAL_SERVICE_ERROR - 3rd party API failure
DATABASE_ERROR - Data persistence issue
INTERNAL_ERROR - Unexpected server error
```

---

## Logging & Audit Architecture

### Logging Levels & Patterns

```
TRACE   - Detailed diagnostic (disabled in production)
DEBUG   - Development troubleshooting
INFO    - Key business events (user login, recipe created)
WARN    - Unexpected but recoverable (slow query, retry)
ERROR   - Error conditions (validation, dependency failure)
FATAL   - System shutdown imminent (DB unreachable)
```

### Structured Logging Format (JSON)

```json
{
  "timestamp": "2026-07-20T07:30:00.000Z",
  "level": "INFO",
  "logger": "RecipeService",
  "message": "Recipe created successfully",
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "organizationId": "550e8400-e29b-41d4-a716-446655440001",
  "recipeId": "550e8400-e29b-41d4-a716-446655440002",
  "duration_ms": 245,
  "traceId": "0HN1GH7F8ASDF",
  "environment": "production",
  "version": "0.2.0"
}
```

### Audit Trail

**Auditable Events:**
- User login / logout
- User role changes
- Data creation (recipes, ingredients)
- Data modifications (updates, deletes)
- Permission changes
- Organization settings changes
- Report generation
- Failed authentication attempts

**Audit Table Structure:**

```sql
CREATE TABLE audit_log (
  id UUID PRIMARY KEY,
  event_type VARCHAR(100),        -- created, updated, deleted, login, etc.
  entity_type VARCHAR(100),       -- recipe, ingredient, user, org, etc.
  entity_id UUID,                 -- target entity UUID
  organization_id UUID,
  user_id UUID,
  old_values JSONB,               -- before state
  new_values JSONB,               -- after state
  ip_address INET,
  user_agent VARCHAR(500),
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
```

### Log Destinations

```
┌────────────────────────────────────┐
│         Application Logs           │
│  (Structured, JSON format)         │
└────────┬──────────────────┬────────┘
         │                  │
    ┌────▼────┐       ┌─────▼──────┐
    │ Console  │       │  File      │
    │ (Dev)    │       │ (Rotating) │
    └──────────┘       └─────┬──────┘
                             │
                ┌────────────┼────────────┐
                │            │            │
           ┌────▼────┐  ┌────▼────┐  ┌───▼────┐
           │   ELK   │  │  Cloud  │  │ Errors │
           │  Stack  │  │ Logging │  │  (e.g. │
           │(Prod)   │  │(Azure)  │  │Sentry) │
           └─────────┘  └─────────┘  └────────┘
```

---

## Data Flow

### Recipe Creation Flow

```
┌─────────────┐
│   Client    │
│ POST /api/v1/recipes
│ { name, cuisine, difficulty }
└─────┬───────┘
      │
      ▼
┌──────────────────────────────────┐
│  API Authentication Middleware   │
│  - Validate JWT                  │
│  - Extract claims (userId, org)  │
└──────┬───────────────────────────┘
      │
      ▼
┌──────────────────────────────────┐
│  Authorization Middleware        │
│  - Check user has "editor" role  │
└──────┬───────────────────────────┘
      │
      ▼
┌──────────────────────────────────┐
│  RecipesController               │
│  - Validate input (Zod schema)   │
│  - Call RecipeService            │
└──────┬───────────────────────────┘
      │
      ▼
┌──────────────────────────────────┐
│  RecipeService                   │
│  - Create Recipe entity          │
│  - Set default values            │
│  - Initialize version history    │
└──────┬───────────────────────────┘
      │
      ▼
┌──────────────────────────────────┐
│  Repository Pattern              │
│  - Insert into database          │
│  - Return persisted entity       │
└──────┬───────────────────────────┘
      │
      ▼
┌──────────────────────────────────┐
│  Transaction Commit              │
│  - Save recipe                   │
│  - Create audit log entry        │
└──────┬───────────────────────────┘
      │
      ▼
┌──────────────────────────────────┐
│  Event Publishing                │
│  - Publish "RecipeCreated" event │
│  - Async notification processing │
└──────┬───────────────────────────┘
      │
      ▼
┌──────────────────────────────────┐
│  Logging                         │
│  - Log success event             │
│  - Include duration, userId      │
└──────┬───────────────────────────┘
      │
      ▼
┌─────────────────────────────────┐
│   Response                      │
│   201 Created                   │
│   { id, name, createdAt, ... }  │
└─────────────────────────────────┘
```

---

## Security Model

### Defense in Depth

```
Layer 1: Network
├── HTTPS/TLS 1.3
├── DDoS Protection (Cloudflare)
├── WAF (Web Application Firewall)
└── Rate Limiting (per API key, IP)

Layer 2: Authentication
├── JWT with RS256 signing
├── Token expiration enforcement
├── Refresh token rotation
└── 2FA support (TOTP)

Layer 3: Authorization
├── Role-Based Access Control (RBAC)
├── Permission checks per operation
├── Tenant isolation (RLS)
└── Resource-level permissions

Layer 4: Data Protection
├── AES-256 encryption at rest
├── TLS in transit
├── Encrypted password hashes (bcrypt)
├── PII field masking in logs
└── Secure secret management

Layer 5: Application
├── Input validation (Zod schemas)
├── Output encoding (JSON escaping)
├── SQL injection prevention (parameterized)
├── XSS protection (CSP headers)
├── CSRF protection (SameSite cookies)
└── Security headers (Helmet)

Layer 6: Monitoring
├── Intrusion detection
├── Anomaly detection
├── Failed login tracking
├── Permission escalation alerts
└── Audit logging
```

---

## Scalability Considerations

### Horizontal Scaling

```
┌───────────────────────────────────┐
│      Kubernetes Cluster           │
│      (Multiple Nodes)             │
└────────┬────────────────┬─────────┘
         │                │
    ┌────▼────┐      ┌────▼────┐
    │ API Pod │      │ API Pod │
    │ (N.NET) │      │ (N.NET) │
    └────┬────┘      └────┬────┘
         │                │
         └────────┬───────┘
                  │
         ┌────────▼────────┐
         │   Load Balancer │
         │   (Round Robin) │
         └────────┬────────┘
                  │
         ┌────────▼────────┐
         │  PostgreSQL     │
         │  (Master/Slave) │
         └────────────────┘
```

### Caching Strategy

```
L1: In-Memory Cache (Redis)
    - JWT blacklist
    - User permissions
    - Recent recipes (5 min TTL)
    - Ingredient search index

L2: Database Query Cache
    - Prepared statements
    - Connection pooling
    - Query result caching

L3: CDN (for static assets)
    - Images
    - Documentation
    - UI assets
```

### Asynchronous Processing

```
Sync Operations:
- Login, CRUD operations (< 100ms)

Async Operations (via message queue):
- Cost calculations (complex formulas)
- Nutrition API calls
- Report generation
- Email notifications
- Audit logging
- AI recommendations
```

---

## Design Patterns Used

| Pattern | Purpose | Example |
|---------|---------|---------|
| **Repository** | Abstract data access | IRecipeRepository |
| **Dependency Injection** | Loose coupling | Constructor injection |
| **Service Layer** | Business logic isolation | RecipeService |
| **Factory** | Create complex objects | EntityFactory |
| **Decorator** | Add behavior dynamically | Logging, caching |
| **Mediator** | Decouple event producers/consumers | MediatR (CQRS) |
| **Specification** | Encapsulate query logic | RecipeSpecification |
| **Unit of Work** | Transaction management | DbContext |
| **Circuit Breaker** | Handle external failures | Polly |
| **Bulkhead** | Resource isolation | Thread pools |

---

## API Endpoints Overview

See `docs/API.md` for complete OpenAPI specification.

### Authentication
- `POST /api/v1/auth/register` - Create account
- `POST /api/v1/auth/login` - Obtain tokens
- `POST /api/v1/auth/refresh` - Refresh access token
- `POST /api/v1/auth/logout` - Revoke tokens
- `POST /api/v1/auth/enable-2fa` - Enable 2FA

### Organizations
- `POST /api/v1/organizations` - Create org
- `GET /api/v1/organizations/{id}` - Get org details
- `PUT /api/v1/organizations/{id}` - Update org
- `GET /api/v1/organizations/{id}/members` - List team members

### Ingredients
- `POST /api/v1/ingredients` - Create
- `GET /api/v1/ingredients` - List
- `GET /api/v1/ingredients/{id}` - Get detail
- `PUT /api/v1/ingredients/{id}` - Update
- `DELETE /api/v1/ingredients/{id}` - Delete

### Recipes
- `POST /api/v1/recipes` - Create
- `GET /api/v1/recipes` - List
- `GET /api/v1/recipes/{id}` - Get detail
- `PUT /api/v1/recipes/{id}` - Update
- `DELETE /api/v1/recipes/{id}` - Delete
- `POST /api/v1/recipes/{id}/publish` - Publish

### Costs
- `GET /api/v1/recipes/{id}/cost` - Calculate cost
- `GET /api/v1/recipes/{id}/cost-history` - Cost history
- `GET /api/v1/recipes/{id}/profit-margin` - Profit analysis

### Inventory
- `GET /api/v1/inventory` - List stock
- `POST /api/v1/inventory/{ingredientId}/add-stock` - Add stock
- `POST /api/v1/inventory/{ingredientId}/consume` - Use stock
- `GET /api/v1/inventory/low-stock` - Low stock alerts

### Reports
- `GET /api/v1/reports` - List reports
- `POST /api/v1/reports/cost-analysis` - Generate cost report
- `GET /api/v1/reports/{id}` - Get report

---

## Next Steps

1. **Package C (Database):** Create Entity Relationship Diagram (ERD)
2. **Package D (API):** Generate OpenAPI 3.0 specification
3. **Package E (Skeleton):** Scaffold ASP.NET Core and Flutter projects

---

**Document Version:** 1.0  
**Last Updated:** 2026-07-20  
**Status:** Ready for Review  
**Approval:** Pending
