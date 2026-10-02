CREATE TABLE IF NOT EXISTS idempotency_records (
    key text PRIMARY KEY,
    fingerprint text NOT NULL,
    status_code integer NOT NULL,
    response_body jsonb NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS audit_events (
    id uuid PRIMARY KEY,
    organization_id uuid NOT NULL,
    branch_id uuid NULL,
    actor_user_id uuid NULL,
    action text NOT NULL,
    entity_type text NOT NULL,
    entity_id uuid NULL,
    before_json jsonb NULL,
    after_json jsonb NULL,
    correlation_id text NOT NULL,
    occurred_at timestamptz NOT NULL
);
