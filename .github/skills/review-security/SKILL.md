---
name: review-security
description: Use this skill when asked to perform a security review or security audit of code in a Cratis-based project. Checks for injection, auth/authz, data exposure, secrets, and event-sourcing-specific vulnerabilities.
---

Perform a structured security review of changed code.

## Input Validation & Injection

- [ ] All command properties validated before use (null, empty, range, format)
- [ ] No raw SQL concatenation — parameterised queries or EF Core only
- [ ] No user-supplied values passed to `Path.Combine`, `File.*`, shell commands, or process args
- [ ] No user-supplied values used as event-store keys without sanitisation

## Authentication & Authorisation

- [ ] All HTTP endpoints decorated with `[Authorize]` or explicitly `[AllowAnonymous]` with justification
- [ ] Tenant isolation enforced — no cross-tenant data accessible without authorisation
- [ ] Claims verified before acting on identity-dependent command data

## Sensitive Data Exposure

- [ ] No passwords, secrets, API keys, or tokens stored in event properties or read models
- [ ] No PII returned to clients that did not provide it
- [ ] Query results scoped to requesting tenant/user — never return all-tenant data

## Secrets & Configuration

- [ ] No secrets in source code, config files, or test fixtures
- [ ] Secrets loaded from environment variables or a secrets manager
- [ ] No hard-coded connection strings in non-test code

## Event Sourcing Specifics

- [ ] Events are immutable records — no mutable state in the event store
- [ ] Aggregate/event-store IDs generated server-side, never accepted from untrusted clients
- [ ] Event upcasting logic does not allow injection of unexpected properties
- [ ] Uniqueness constraints cannot be bypassed by concurrent multi-tenant writes

## Frontend

- [ ] No user-supplied values in `dangerouslySetInnerHTML`
- [ ] No tokens or secrets in `localStorage` — use `httpOnly` cookies or in-memory state
- [ ] Command DTOs contain only the minimum required fields
- [ ] No client-side access control not also enforced server-side

## Risk classification

- 🔴 Critical — must fix before merge
- 🟡 Medium — should fix soon
- 🟢 Low — fix when convenient

## Output format

Start with: **Security Review: ✅ No issues / ⚠️ Low-risk findings / ❌ Blocking issues found**

Group findings by category. End with a summary table showing ✅/⚠️/❌ per category.
