# GastronomIQ Database Design

**Version:** 1.0  
**Last Updated:** 2026-07-20  
**Status:** Ready for Implementation  
**Target Database:** PostgreSQL 14+

---

## Table of Contents

1. [Overview](#overview)
2. [Entity Relationship Diagram](#entity-relationship-diagram)
3. [Database Naming Conventions](#database-naming-conventions)
4. [Core Entities](#core-entities)
5. [Complete Schema](#complete-schema)
6. [Indexes & Performance](#indexes--performance)
7. [Constraints & Referential Integrity](#constraints--referential-integrity)
8. [Migration Strategy](#migration-strategy)
9. [Data Dictionary](#data-dictionary)
10. [Backup & Recovery](#backup--recovery)

---

## Overview

### Design Principles

- **Normalization:** 3NF (Third Normal Form) with strategic denormalization for performance
- **Multi-Tenancy:** All data scoped to `organization_id`
- **Audit Trail:** All tables include `created_at`, `updated_at`, `created_by`
- **Soft Deletes:** `deleted_at` column for data recovery
- **Row-Level Security:** PostgreSQL RLS policies for tenant isolation
- **Temporal Data:** Version history for recipes and cost changes
- **Referential Integrity:** Foreign key constraints with CASCADE rules

### Database Statistics (Estimated)

| Entity | Rows/Org | Growth | Notes |
|--------|----------|--------|-------|
| Organizations | 1 | - | Single per tenant |
| Users | 5-50 | Linear | Team size |
| Roles | 5 | Static | Fixed set |
| Ingredients | 500-5000 | Logarithmic | Shared across recipes |
| Recipes | 100-1000 | Linear | Core business data |
| Recipe Steps | 300-3000 | Linear | Steps per recipe |
| Recipe Ingredients | 1000-5000 | Linear | Ingredients per recipe |
| Inventory Items | 500-5000 | Linear | One per ingredient |
| Cost History | 1000-10000 | Linear | Cost tracking |
| Audit Logs | 100000+ | Rapid | Compliance |

---

## Entity Relationship Diagram

### Conceptual Model

```
┌──────────────────────────────────────────────────────────────────┐
│                        ORGANIZATIONS                              │
│                    (Multi-Tenant Context)                         │
│  id (PK) | name | slug | industry | country | plan | created_at  │
└────────────────────┬─────────────────────────────────────────────┘
                     │
        ┌────────────┼────────────┐
        │            │            │
        ▼            ▼            ▼
    ┌────────┐  ┌───────┐  ┌──────────────┐
    │ USERS  │  │ ROLES │  │ PERMISSIONS  │
    └────────┘  └───────┘  └──────────────┘
        │            │            │
        │            └────────────┘
        │                 │
        ▼                 ▼
    ┌──────────────┐
    │ USER_ROLES   │ (Junction: users ← m:n → roles)
    └──────────────┘


    ┌────────────────┐        ┌──────────────────┐
    │ INGREDIENTS    │◄─────m:n─────────┤ CATEGORIES   │
    │                │        │          │               │
    │ - name         │        │          │ - name        │
    │ - category_id  │        │          │ - description │
    │ - is_allergen  │        │          └──────────────┘
    │ - unit         │
    │ - cost         │
    └────────┬───────┘
             │
             │
    ┌────────▼────────────────────────┐
    │ RECIPES                          │
    │                                  │
    │ - name                           │
    │ - description                    │
    │ - difficulty                     │
    │ - cuisine                        │
    │ - prep_time_minutes              │
    │ - cook_time_minutes              │
    │ - servings                       │
    │ - status (draft/published)       │
    │ - created_by (user_id)           │
    └────┬─────────────────────────────┘
         │
    ┌────┴────────────────────┬──────────────────────────┐
    │                         │                          │
    ▼                         ▼                          ▼
┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│ RECIPE_STEPS     │  │ RECIPE_          │  │ NUTRITION_INFO   │
│                  │  │ INGREDIENTS      │  │                  │
│ - step_number    │  │                  │  │ - calories       │
│ - instructions   │  │ - ingredient_id  │  │ - protein_g      │
│ - duration_min   │  │ - quantity       │  │ - carbs_g        │
│ - notes          │  │ - unit           │  │ - fat_g          │
│ - equipment      │  │ - notes          │  │ - fiber_g        │
└──────────────────┘  └────────┬─────────┘  └──────────────────┘
                               │
                        ┌──────▼────────┐
                        │ RECIPE_COSTS   │
                        │                │
                        │ - cost_per_    │
                        │   serving      │
                        │ - total_cost   │
                        │ - currency     │
                        │ - calculated_  │
                        │   at           │
                        └────────────────┘

    ┌─────────────────────────────────┐
    │ INVENTORY                       │
    │                                 │
    │ - ingredient_id                 │
    │ - quantity_on_hand              │
    │ - unit                          │
    │ - reorder_point                 │
    │ - reorder_quantity              │
    │ - unit_cost                     │
    │ - last_restocked_at             │
    │ - supplier_id                   │
    └──────────────────┬──────────────┘
                       │
            ┌──────────┴──────────┐
            │                     │
            ▼                     ▼
    ┌──────────────────┐  ┌──────────────────┐
    │ STOCK_MOVEMENTS  │  │ INVENTORY_       │
    │                  │  │ ALERTS           │
    │ - movement_type  │  │                  │
    │ - quantity       │  │ - alert_type     │
    │ - reason         │  │ - is_resolved    │
    │ - reference_id   │  │ - resolved_at    │
    └──────────────────┘  └──────────────────┘

    ┌──────────────────────────────────┐
    │ COMMENTS                         │
    │                                  │
    │ - recipe_id                      │
    │ - user_id                        │
    │ - content                        │
    │ - created_at                     │
    │ - updated_at                     │
    └──────────────────────────────────┘

    ┌──────────────────────────────────┐
    │ RATINGS                          │
    │                                  │
    │ - recipe_id                      │
    │ - user_id                        │
    │ - rating (1-5)                   │
    │ - review_text                    │
    │ - created_at                     │
    └──────────────────────────────────┘

    ┌──────────────────────────────────┐
    │ FAVORITES                        │
    │                                  │
    │ - recipe_id                      │
    │ - user_id                        │
    │ - created_at                     │
    └──────────────────────────────────┘

    ┌──────────────────────────────────┐
    │ AUDIT_LOG                        │
    │                                  │
    │ - event_type                     │
    │ - entity_type                    │
    │ - entity_id                      │
    │ - user_id                        │
    │ - old_values (JSONB)             │
    │ - new_values (JSONB)             │
    │ - created_at                     │
    └──────────────────────────────────┘
```

---

## Database Naming Conventions

### Table Names
- **Plural, lowercase, snake_case:** `users`, `recipes`, `recipe_steps`
- **Junction tables:** `{entity1}_{entity2}` (e.g., `users_roles`)
- **Avoid:** Reserved keywords, abbreviations, prefixes (tbl_, t_)

### Column Names
- **Lowercase, snake_case:** `first_name`, `created_at`, `is_active`
- **Booleans:** Prefix with `is_` or `has_`: `is_active`, `has_deleted_at`
- **Foreign keys:** `{table_singular}_id` (e.g., `user_id`, `recipe_id`)
- **Primary keys:** Always `id` (UUID)
- **Timestamps:** `created_at`, `updated_at`, `deleted_at`
- **Audit columns:** `created_by`, `updated_by`

### Indexes
- **Format:** `idx_{table}_{columns}` (e.g., `idx_recipes_organization_id_status`)
- **Unique indexes:** `uq_{table}_{columns}` (e.g., `uq_users_email`)

### Constraints
- **Primary key:** `pk_{table}` (e.g., `pk_users`)
- **Foreign key:** `fk_{table}_{referenced_table}` (e.g., `fk_recipes_users`)
- **Unique:** `uq_{table}_{columns}`
- **Check:** `ck_{table}_{constraint}` (e.g., `ck_recipes_difficulty`)

---

## Core Entities

### Organizations
Multi-tenant root entity. One per customer.

```
id (UUID, PK)
name (VARCHAR 255, NOT NULL, UNIQUE per industry)
slug (VARCHAR 100, NOT NULL, UNIQUE) - URL-friendly identifier
industry (VARCHAR 50) - restaurant, catering, food_manufacturing, etc.
country (VARCHAR 2) - ISO 3166-1 alpha-2
website (VARCHAR 255)
billing_plan (VARCHAR 50) - free, starter, professional, enterprise
max_users (INT) - based on plan
max_storage_gb (INT) - based on plan
is_active (BOOLEAN DEFAULT true)
created_at (TIMESTAMP WITH TIME ZONE)
updated_at (TIMESTAMP WITH TIME ZONE)
deleted_at (TIMESTAMP WITH TIME ZONE, nullable)
```

### Users
Team members within organizations.

```
id (UUID, PK)
organization_id (UUID, FK → organizations)
email (VARCHAR 255, NOT NULL)
password_hash (VARCHAR 255, NOT NULL) - bcrypt hashed
first_name (VARCHAR 100, NOT NULL)
last_name (VARCHAR 100, NOT NULL)
phone (VARCHAR 20)
avatar_url (VARCHAR 500)
is_active (BOOLEAN DEFAULT true)
email_verified (BOOLEAN DEFAULT false)
email_verified_at (TIMESTAMP WITH TIME ZONE)
two_factor_enabled (BOOLEAN DEFAULT false)
last_login_at (TIMESTAMP WITH TIME ZONE)
created_at (TIMESTAMP WITH TIME ZONE)
updated_at (TIMESTAMP WITH TIME ZONE)
deleted_at (TIMESTAMP WITH TIME ZONE)

UNIQUE (organization_id, email) - email unique per org
```

### Roles
Predefined role types for RBAC.

```
id (UUID, PK)
name (VARCHAR 50, NOT NULL) - owner, admin, editor, viewer
description (TEXT)
is_system (BOOLEAN DEFAULT true) - system roles can't be deleted
created_at (TIMESTAMP WITH TIME ZONE)

Values: owner, admin, editor, viewer, custom
```

### Ingredients
Master ingredient database.

```
id (UUID, PK)
organization_id (UUID, FK → organizations)
name (VARCHAR 255, NOT NULL)
description (TEXT)
category_id (UUID, FK → categories, NULLABLE)
unit (VARCHAR 20) - g, ml, cup, tbsp, tsp, piece, etc.
is_allergen (BOOLEAN DEFAULT false)
allergens (TEXT[]) - array of allergen types
cost_per_unit (DECIMAL 12,4) - cached cost
supplier_id (UUID, FK → suppliers, NULLABLE)
supplier_code (VARCHAR 50)
is_active (BOOLEAN DEFAULT true)
created_at (TIMESTAMP WITH TIME ZONE)
updated_at (TIMESTAMP WITH TIME ZONE)
created_by (UUID, FK → users)
deleted_at (TIMESTAMP WITH TIME ZONE)

UNIQUE (organization_id, name, unit)
```

### Recipes
Recipe definitions and metadata.

```
id (UUID, PK)
organization_id (UUID, FK → organizations)
name (VARCHAR 255, NOT NULL)
description (TEXT)
created_by (UUID, FK → users)
status (VARCHAR 50) - draft, review, approved, published, archived
difficulty (VARCHAR 20) - easy, medium, hard, expert
cuisine (VARCHAR 50) - italian, french, spanish, mexican, asian, etc.
yield_amount (DECIMAL 10,2) - e.g., 4 servings
yield_unit (VARCHAR 20) - servings, pieces, portions
prep_time_minutes (INT)
cook_time_minutes (INT)
rest_time_minutes (INT, NULLABLE)
total_time_minutes (INT, GENERATED) - sum of above
dietary_restrictions (TEXT[]) - vegan, vegetarian, gluten-free, etc.
equipment (TEXT[]) - oven, mixer, blender, etc.
version_number (INT DEFAULT 1)
is_published (BOOLEAN DEFAULT false)
published_at (TIMESTAMP WITH TIME ZONE, NULLABLE)
view_count (INT DEFAULT 0)
like_count (INT DEFAULT 0)
created_at (TIMESTAMP WITH TIME ZONE)
updated_at (TIMESTAMP WITH TIME ZONE)
created_by_id (UUID, FK → users)
deleted_at (TIMESTAMP WITH TIME ZONE)

UNIQUE (organization_id, name, version_number)
```

### Recipe Steps
Instructions for recipe preparation.

```
id (UUID, PK)
recipe_id (UUID, FK → recipes, ON DELETE CASCADE)
step_number (INT, NOT NULL)
title (VARCHAR 255, NULLABLE)
instructions (TEXT, NOT NULL)
duration_minutes (INT, NULLABLE)
temperature_celsius (DECIMAL 5,1, NULLABLE)
equipment_needed (TEXT[])
notes (TEXT)
created_at (TIMESTAMP WITH TIME ZONE)
updated_at (TIMESTAMP WITH TIME ZONE)

UNIQUE (recipe_id, step_number)
```

### Recipe Ingredients
Junction table linking recipes and ingredients with quantities.

```
id (UUID, PK)
recipe_id (UUID, FK → recipes, ON DELETE CASCADE)
ingredient_id (UUID, FK → ingredients)
sequence_number (INT) - order in recipe
quantity (DECIMAL 10,2, NOT NULL)
unit (VARCHAR 20, NOT NULL) - overrides ingredient default
notes (TEXT) - e.g., "finely minced", "at room temperature"
is_optional (BOOLEAN DEFAULT false)
substitutes (JSONB) - alternative ingredients
created_at (TIMESTAMP WITH TIME ZONE)
updated_at (TIMESTAMP WITH TIME ZONE)

UNIQUE (recipe_id, ingredient_id)
```

### Nutrition Info
Calculated nutritional data per recipe.

```
id (UUID, PK)
recipe_id (UUID, FK → recipes, ON DELETE CASCADE)
per_serving (BOOLEAN DEFAULT true)
calories (DECIMAL 10,2)
protein_g (DECIMAL 10,2)
carbohydrates_g (DECIMAL 10,2)
fat_g (DECIMAL 10,2)
saturated_fat_g (DECIMAL 10,2)
trans_fat_g (DECIMAL 10,2)
fiber_g (DECIMAL 10,2)
sugar_g (DECIMAL 10,2)
sodium_mg (DECIMAL 10,2)
cholesterol_mg (DECIMAL 10,2)
calcium_mg (DECIMAL 10,2)
iron_mg (DECIMAL 10,2)
calculated_at (TIMESTAMP WITH TIME ZONE)
calculation_method (VARCHAR 50) - usda, manual, estimated

UNIQUE (recipe_id)
```

### Recipe Costs
Cost tracking and history.

```
id (UUID, PK)
recipe_id (UUID, FK → recipes)
ingredient_costs (JSONB) - { ingredient_id: cost_per_unit }
total_cost (DECIMAL 12,4)
cost_per_serving (DECIMAL 12,4)
currency (VARCHAR 3 DEFAULT 'USD')
calculated_at (TIMESTAMP WITH TIME ZONE)
created_at (TIMESTAMP WITH TIME ZONE)

INDEX (recipe_id, calculated_at)
```

### Inventory
Current stock levels.

```
id (UUID, PK)
organization_id (UUID, FK → organizations)
ingredient_id (UUID, FK → ingredients)
quantity_on_hand (DECIMAL 12,4, NOT NULL)
unit (VARCHAR 20, NOT NULL)
reorder_point (DECIMAL 12,4) - trigger for reorder alerts
reorder_quantity (DECIMAL 12,4) - standard reorder amount
unit_cost (DECIMAL 12,4) - current cost per unit
last_restocked_at (TIMESTAMP WITH TIME ZONE)
supplier_id (UUID, NULLABLE) - preferred supplier
supplier_code (VARCHAR 50)
storage_location (VARCHAR 100) - shelf, freezer, etc.
expiration_date (DATE, NULLABLE)
batch_number (VARCHAR 50, NULLABLE)
notes (TEXT)
created_at (TIMESTAMP WITH TIME ZONE)
updated_at (TIMESTAMP WITH TIME ZONE)

UNIQUE (organization_id, ingredient_id)
CHECK (quantity_on_hand >= 0)
```

### Stock Movements
Audit trail for inventory changes.

```
id (UUID, PK)
organization_id (UUID, FK → organizations)
inventory_id (UUID, FK → inventory)
ingredient_id (UUID, FK → ingredients)
movement_type (VARCHAR 50) - add, consume, adjust, return, damage
quantity (DECIMAL 12,4, NOT NULL) - signed value
unit (VARCHAR 20, NOT NULL)
reason (TEXT) - "Received order #123", "Used in Recipe ABC", etc.
reference_type (VARCHAR 50) - purchase_order, recipe, adjustment, etc.
reference_id (UUID, NULLABLE)
created_by (UUID, FK → users)
created_at (TIMESTAMP WITH TIME ZONE)

INDEX (organization_id, created_at)
```

### Comments
Collaboration on recipes.

```
id (UUID, PK)
recipe_id (UUID, FK → recipes, ON DELETE CASCADE)
user_id (UUID, FK → users)
content (TEXT, NOT NULL)
is_resolved (BOOLEAN DEFAULT false)
resolved_at (TIMESTAMP WITH TIME ZONE, NULLABLE)
created_at (TIMESTAMP WITH TIME ZONE)
updated_at (TIMESTAMP WITH TIME ZONE)

INDEX (recipe_id, created_at DESC)
```

### Ratings & Favorites
User engagement.

```
ratings:
  id (UUID, PK)
  recipe_id (UUID, FK → recipes)
  user_id (UUID, FK → users)
  rating (INT, NOT NULL) CHECK (rating BETWEEN 1 AND 5)
  review_text (TEXT, NULLABLE)
  created_at (TIMESTAMP WITH TIME ZONE)
  updated_at (TIMESTAMP WITH TIME ZONE)
  UNIQUE (recipe_id, user_id)

favorites:
  id (UUID, PK)
  recipe_id (UUID, FK → recipes)
  user_id (UUID, FK → users)
  created_at (TIMESTAMP WITH TIME ZONE)
  UNIQUE (recipe_id, user_id)
```

### Audit Log
Comprehensive audit trail.

```
id (UUID, PK)
organization_id (UUID, FK → organizations, NULLABLE for system events)
user_id (UUID, FK → users, NULLABLE for system events)
event_type (VARCHAR 100) - created, updated, deleted, login, export, etc.
entity_type (VARCHAR 100) - recipe, ingredient, user, organization, etc.
entity_id (UUID) - the affected record
old_values (JSONB) - before state
new_values (JSONB) - after state
ip_address (INET, NULLABLE)
user_agent (VARCHAR 500, NULLABLE)
changes_summary (TEXT) - human-readable summary
created_at (TIMESTAMP WITH TIME ZONE)

INDEX (organization_id, created_at DESC)
INDEX (event_type, created_at DESC)
INDEX (entity_type, entity_id)
```

---

## Complete Schema

See `migrations/001_initial_schema.sql` (already committed to repo)

---

## Indexes & Performance

### Critical Indexes for Multi-Tenancy

```sql
-- Organization-scoped queries (most common)
CREATE INDEX idx_users_organization_id 
  ON users(organization_id) 
  WHERE deleted_at IS NULL;

CREATE INDEX idx_recipes_organization_id_status 
  ON recipes(organization_id, status) 
  WHERE deleted_at IS NULL;

CREATE INDEX idx_ingredients_organization_id 
  ON ingredients(organization_id) 
  WHERE deleted_at IS NULL;

CREATE INDEX idx_inventory_organization_id 
  ON inventory(organization_id);

-- Search/Filter indexes
CREATE INDEX idx_recipes_name_trgm 
  ON recipes USING GIN(name gin_trgm_ops);

CREATE INDEX idx_ingredients_name_trgm 
  ON ingredients USING GIN(name gin_trgm_ops);

-- Foreign key indexes (automatic for PK, but needed for lookups)
CREATE INDEX idx_recipes_created_by 
  ON recipes(created_by);

CREATE INDEX idx_recipe_ingredients_ingredient_id 
  ON recipe_ingredients(ingredient_id);

-- Time-series queries
CREATE INDEX idx_stock_movements_organization_created 
  ON stock_movements(organization_id, created_at DESC);

CREATE INDEX idx_audit_log_organization_created 
  ON audit_log(organization_id, created_at DESC);

-- Unique constraints (automatically indexed)
CREATE UNIQUE INDEX uq_users_organization_email 
  ON users(organization_id, email) 
  WHERE deleted_at IS NULL;

CREATE UNIQUE INDEX uq_inventory_org_ingredient 
  ON inventory(organization_id, ingredient_id);
```

### Index Statistics

```
Estimated indexes: 25-30
Maintenance strategy: Analyze weekly, REINDEX monthly
Bloat threshold: 30% (trigger VACUUM FULL)
```

---

## Constraints & Referential Integrity

### Foreign Key Strategy

```sql
-- Cascading deletes for dependent data
CONSTRAINT fk_recipe_steps_recipes 
  FOREIGN KEY (recipe_id) REFERENCES recipes(id) 
  ON DELETE CASCADE 
  ON UPDATE CASCADE;

CONSTRAINT fk_recipe_ingredients_recipes 
  FOREIGN KEY (recipe_id) REFERENCES recipes(id) 
  ON DELETE CASCADE 
  ON UPDATE CASCADE;

-- Restrict deletes for shared data (ingredients, users)
CONSTRAINT fk_recipes_users 
  FOREIGN KEY (created_by) REFERENCES users(id) 
  ON DELETE RESTRICT 
  ON UPDATE CASCADE;

CONSTRAINT fk_recipe_ingredients_ingredients 
  FOREIGN KEY (ingredient_id) REFERENCES ingredients(id) 
  ON DELETE RESTRICT 
  ON UPDATE CASCADE;
```

### Check Constraints

```sql
-- Numeric ranges
ALTER TABLE recipes 
  ADD CONSTRAINT ck_recipes_prep_time CHECK (prep_time_minutes >= 0);

ALTER TABLE recipes 
  ADD CONSTRAINT ck_recipes_cook_time CHECK (cook_time_minutes >= 0);

ALTER TABLE ratings 
  ADD CONSTRAINT ck_ratings_range CHECK (rating BETWEEN 1 AND 5);

ALTER TABLE recipe_ingredients 
  ADD CONSTRAINT ck_recipe_ingredients_qty CHECK (quantity > 0);

-- Enum values
ALTER TABLE recipes 
  ADD CONSTRAINT ck_recipes_status 
  CHECK (status IN ('draft', 'review', 'approved', 'published', 'archived'));

ALTER TABLE recipes 
  ADD CONSTRAINT ck_recipes_difficulty 
  CHECK (difficulty IN ('easy', 'medium', 'hard', 'expert'));

ALTER TABLE stock_movements 
  ADD CONSTRAINT ck_movement_type 
  CHECK (movement_type IN ('add', 'consume', 'adjust', 'return', 'damage'));
```

---

## Migration Strategy

### Version Control Approach

```
migrations/
├── 001_initial_schema.sql          (2026-07-20)
├── 002_add_recipe_versions.sql     (Planned)
├── 003_add_cost_history.sql        (Planned)
├── 004_add_row_level_security.sql  (Planned)
└── 005_add_full_text_search.sql    (Planned)
```

### Migration Characteristics

- **Idempotent:** Safe to rerun (use `CREATE TABLE IF NOT EXISTS`)
- **Backward compatible:** No breaking changes without major version
- **Testable:** Run against test database before production
- **Documented:** Include rollback procedures

### Rollback Strategy

```
For each migration, include rollback SQL:

-- ROLLBACK (if needed)
-- DROP TABLE IF EXISTS table_name CASCADE;
-- DROP INDEX IF EXISTS index_name;
-- ALTER TABLE parent_table DROP CONSTRAINT fk_name;
```

---

## Data Dictionary

### Organizations Table

| Column | Type | Nullable | Constraints | Description |
|--------|------|----------|-------------|-------------|
| id | UUID | NO | PK | Unique identifier |
| name | VARCHAR(255) | NO | UNIQUE | Organization name |
| slug | VARCHAR(100) | NO | UNIQUE | URL-friendly identifier |
| industry | VARCHAR(50) | YES | - | Industry type |
| country | VARCHAR(2) | YES | - | ISO country code |
| website | VARCHAR(255) | YES | - | Organization website |
| billing_plan | VARCHAR(50) | NO | - | Subscription tier |
| max_users | INT | NO | - | User limit |
| max_storage_gb | INT | NO | - | Storage limit |
| is_active | BOOLEAN | NO | DEFAULT true | Active flag |
| created_at | TIMESTAMP | NO | DEFAULT NOW() | Creation timestamp |
| updated_at | TIMESTAMP | NO | DEFAULT NOW() | Last update |
| deleted_at | TIMESTAMP | YES | - | Soft delete |

### Users Table

| Column | Type | Nullable | Constraints | Description |
|--------|------|----------|-------------|-------------|
| id | UUID | NO | PK | Unique identifier |
| organization_id | UUID | NO | FK | Organization owner |
| email | VARCHAR(255) | NO | - | Email address |
| password_hash | VARCHAR(255) | NO | - | bcrypt hash |
| first_name | VARCHAR(100) | NO | - | First name |
| last_name | VARCHAR(100) | NO | - | Last name |
| phone | VARCHAR(20) | YES | - | Phone number |
| avatar_url | VARCHAR(500) | YES | - | Profile picture URL |
| is_active | BOOLEAN | NO | DEFAULT true | Active flag |
| email_verified | BOOLEAN | NO | DEFAULT false | Email verified |
| email_verified_at | TIMESTAMP | YES | - | Verification time |
| two_factor_enabled | BOOLEAN | NO | DEFAULT false | 2FA enabled |
| last_login_at | TIMESTAMP | YES | - | Last login |
| created_at | TIMESTAMP | NO | DEFAULT NOW() | Creation timestamp |
| updated_at | TIMESTAMP | NO | DEFAULT NOW() | Last update |
| deleted_at | TIMESTAMP | YES | - | Soft delete |

### Recipes Table

| Column | Type | Nullable | Constraints | Description |
|--------|------|----------|-------------|-------------|
| id | UUID | NO | PK | Unique identifier |
| organization_id | UUID | NO | FK | Owner organization |
| name | VARCHAR(255) | NO | - | Recipe name |
| description | TEXT | YES | - | Recipe description |
| created_by | UUID | NO | FK | Creator user |
| status | VARCHAR(50) | NO | CHECK | Publishing status |
| difficulty | VARCHAR(20) | NO | CHECK | Difficulty level |
| cuisine | VARCHAR(50) | YES | - | Cuisine type |
| yield_amount | DECIMAL(10,2) | NO | - | Serving size |
| yield_unit | VARCHAR(20) | NO | - | Unit of yield |
| prep_time_minutes | INT | YES | CHECK | Prep duration |
| cook_time_minutes | INT | YES | CHECK | Cook duration |
| rest_time_minutes | INT | YES | - | Rest/chill time |
| total_time_minutes | INT | YES | GENERATED | Total time |
| dietary_restrictions | TEXT[] | YES | - | Dietary flags |
| equipment | TEXT[] | YES | - | Equipment needed |
| version_number | INT | NO | DEFAULT 1 | Version |
| is_published | BOOLEAN | NO | DEFAULT false | Published flag |
| published_at | TIMESTAMP | YES | - | Publication date |
| view_count | INT | NO | DEFAULT 0 | View count |
| like_count | INT | NO | DEFAULT 0 | Like count |
| created_at | TIMESTAMP | NO | DEFAULT NOW() | Creation timestamp |
| updated_at | TIMESTAMP | NO | DEFAULT NOW() | Last update |
| deleted_at | TIMESTAMP | YES | - | Soft delete |

### Inventory Table

| Column | Type | Nullable | Constraints | Description |
|--------|------|----------|-------------|-------------|
| id | UUID | NO | PK | Unique identifier |
| organization_id | UUID | NO | FK | Owner organization |
| ingredient_id | UUID | NO | FK | Ingredient reference |
| quantity_on_hand | DECIMAL(12,4) | NO | CHECK | Current stock |
| unit | VARCHAR(20) | NO | - | Unit of measure |
| reorder_point | DECIMAL(12,4) | YES | - | Reorder threshold |
| reorder_quantity | DECIMAL(12,4) | YES | - | Standard order qty |
| unit_cost | DECIMAL(12,4) | NO | - | Cost per unit |
| last_restocked_at | TIMESTAMP | YES | - | Last restock date |
| supplier_id | UUID | YES | - | Preferred supplier |
| supplier_code | VARCHAR(50) | YES | - | Supplier SKU |
| storage_location | VARCHAR(100) | YES | - | Physical location |
| expiration_date | DATE | YES | - | Expiry date |
| batch_number | VARCHAR(50) | YES | - | Batch identifier |
| notes | TEXT | YES | - | Additional notes |
| created_at | TIMESTAMP | NO | DEFAULT NOW() | Creation timestamp |
| updated_at | TIMESTAMP | NO | DEFAULT NOW() | Last update |

---

## Backup & Recovery

### Backup Strategy

```bash
# Full backup
pg_dump -Fc gastronomiq > gastronomiq_$(date +%Y%m%d).dump

# Point-in-time recovery (with WAL)
pg_basebackup -D /backup/base -Ft -z -P

# Incremental with WAL archiving
archive_command = 'aws s3 cp %p s3://gastronomiq-backups/wal/%f'
```

### Backup Schedule

| Type | Frequency | Retention | Destination |
|------|-----------|-----------|-------------|
| Full | Daily (2 AM) | 30 days | AWS S3 |
| Transaction Log (WAL) | Continuous | 7 days | AWS S3 |
| Snapshot | Weekly | 12 weeks | AWS EBS |

### Recovery Procedures

```
1. Locate latest backup: ls -ltr /backups/
2. Restore to new instance: pg_restore -d gastronomiq backup.dump
3. Verify data integrity: SELECT COUNT(*) FROM recipes;
4. Resume application traffic
```

---

## Performance Considerations

### Query Optimization

1. **Always filter by organization_id first** - enables partition pruning
2. **Use indexes for frequent WHERE clauses** - see indexes section
3. **Denormalize read-heavy data** - cost_per_serving cached in recipes
4. **Archive old audit logs** - move to separate retention table after 1 year
5. **Use JSONB for flexible metadata** - allergens, equipment arrays

### Connection Pooling

```
Development: pgbouncer (50 connections)
Production: pgbouncer (500 connections)
Mode: transaction pooling for stateless API
```

### Scaling Strategy

```
Phase 1: Single PostgreSQL instance (up to 1M recipes)
Phase 2: Read replicas for reporting queries
Phase 3: Sharding by organization_id if needed
Phase 4: Time-series database for audit logs
```

---

## Row-Level Security (RLS)

### Multi-Tenant Isolation

```sql
-- Enable RLS on all tables
ALTER TABLE recipes ENABLE ROW LEVEL SECURITY;
ALTER TABLE ingredients ENABLE ROW LEVEL SECURITY;
ALTER TABLE inventory ENABLE ROW LEVEL SECURITY;

-- Tenant isolation policy
CREATE POLICY recipes_tenant_isolation ON recipes
  USING (organization_id = current_setting('app.organization_id')::uuid);

-- Per request, set context:
SET app.organization_id = '550e8400-e29b-41d4-a716-446655440000';
SELECT * FROM recipes; -- Only returns this org's recipes
```

---

## Validation Rules

### Business Logic Constraints

| Entity | Rule | Enforcement |
|--------|------|-------------|
| Recipes | Total time = prep + cook + rest | GENERATED COLUMN |
| Recipes | Status workflow | Application + CHECK |
| Ingredients | Cost must be positive | CHECK (cost > 0) |
| Inventory | Stock never negative | CHECK (qty >= 0) |
| Ratings | Between 1-5 | CHECK (rating BETWEEN 1 AND 5) |
| Users | Email must be valid | Application regex |
| Recipes | At least 1 ingredient | Application validation |

---

## Next Steps

1. **Execute 001_initial_schema.sql** in PostgreSQL 14+
2. **Set up Row-Level Security policies** per tenant
3. **Configure backups** to S3 or backup service
4. **Run performance tests** before production
5. **Generate test data** for development environment

---

**Document Version:** 1.0  
**Last Updated:** 2026-07-20  
**Status:** Ready for Implementation  
**Approval:** Pending
