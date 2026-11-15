---
name: cratis-screenplay-authentication-and-onboarding
description: Use when a `.play` model declares how users sign in or get into an application, meaning `authentication` providers, the `onboarding` block (signup, invitations, legal terms, emails) or the environment keys a provider or mail server needs. Not for personas and policies, or for running the application.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-screenplay-authentication-and-onboarding/SKILL.md -->

# Screenplay authentication and onboarding

Two top-level blocks say how people reach an application. `authentication` names the identity
providers. `onboarding` says how people get in: self-signup, who invites whom, which terms they
accept and which emails they receive. Both are authoring metadata, not executable behavior: the
reference evaluator does not run them, and whatever runs the application provisions them.

**Requires** Screenplay 4.259.0 or later for `onboarding`, the environment key listing and its
diagnostics (`screenplay --version`; the compiler bundled in `cratis` may be older). Older
releases reject the block as an unknown top-level construct.

## The line the model never crosses

The model is the same document in every environment. A client id, a client secret, an authority,
a sender address, a mail server and its password differ per environment, so none of them appears
in the model. A configuration line under a provider is a compile error (`PLAY0183`), not an
ignored line. The model refers to a value by a **key derived from its declarations**, and the
environment supplies the value. Never add the value to the model to "make it render". Creating
and filling the environment is [cratis-environments-and-secrets](../cratis-environments-and-secrets/SKILL.md).

## `authentication`

```text
authentication
  provider EntraId
  provider GitHub
  provider OpenId name Partner
  provider OpenId name Supplier
```

- At most one block for the application (`PLAY0125`); the line carries only the keyword (`PLAY0124`).
- Providers are `EntraId`, `GitHub`, `Google`, `Apple` and `OpenId`.
- A provider's **identity** is its `name` alias when it has one, otherwise the provider name.
  Two entries with one identity are `PLAY0169`, so two `OpenId` providers each need a `name`.
- Typed information about the caller is the separate `identity` block, not part of `authentication`.

## `onboarding`

```text
onboarding
  signup open creates tenant
  invite tenant as Owner
  invite member
    by Owner, Administrator
    grant Accountant, InvoiceManager
  legal terms
    file Legal/terms-of-service.md
    file Legal/privacy-policy.md
  offering Starter
  emails
    invitation file Emails/invitation.html
    welcome file Emails/welcome.html
    signupDeclined file Emails/signup-declined.html
    images Emails/Images
```

Read [onboarding directives](references/onboarding-directives.md) before writing or reviewing a
block. The diagnostics for each rule are in that reference. The rules that decide most mistakes:

- It needs `authentication` in the same application, and there is at most one block.
- Every line is optional and appears once. Anything that is not a directive is a compile error.
- **No `signup` line means self-signup is off**; every newcomer arrives by invitation.
  `signup open creates tenant` makes a signup create a tenant, `signup open joins tenant` asks to join an existing one.
- Every persona in `as`, `by` and `grant` must be declared with `persona`; there are no free-text roles.
- `emails` names only `invitation`, `welcome` and `signupDeclined`. The sender and mail server are not in the model.
- `file` paths are repository-relative, like handler files. `offering` is the name of an offer the operator publishes in Box Office; a rendered application starts each new tenant on it. `signupDeclined` is sent when the host declines a held sign-up (review is not modeled; see `cratis-onboarding-runtime`).

## Environment keys

Every key is derived from a declaration, so nothing is configured twice. Keys are dotted,
case-sensitive paths.

| Declaration | Keys | Secret |
|---|---|---|
| each provider | `authentication.providers.<Identity>.clientId` | no |
| each provider | `authentication.providers.<Identity>.clientSecret` | **yes** |
| `EntraId` | `authentication.providers.<Identity>.tenantId` | no |
| `OpenId` | `authentication.providers.<Identity>.authority` | no |
| `onboarding` | `onboarding.email.sender`, `onboarding.email.server.host`, `.port`, `.username` | no |
| `onboarding` | `onboarding.email.server.password` | **yes** |

List the keys for the model instead of working them out by hand:

```bash
screenplay environment ./model                 # one key per line, secrets marked "(secret)"
screenplay environment ./model --format json   # key, secret, source, owner, description
```

Pass the folder, not one file. It exits `0` when it listed keys, `1` when the model does not
compile (it lists nothing) and `2` when it could not run. Compare an environment with the model's
keys using `cratis config --missing <path>`, which exits `5` when any key is absent.

## Verify

1. `screenplay <folder> --warnaserror` exits `0` with none of the diagnostics in the directives reference.
2. The key listing for the folder contains exactly the keys the table above predicts for the providers and the `onboarding` block.
3. `grep -rn -i -E "clientSecret|password|authority" <folder>` finds no value in any `.play` file.

## Route near misses

- Personas, policies and `require`: `cratis-screenplay-command-surface`.
- Choosing a compiler and reading verdicts: `cratis-screenplay-toolchain`.
- Creating an environment, setting a secret: `cratis-environments-and-secrets`.
- How AuthProxy, Ante and Herald consume the contract at runtime: `cratis-onboarding-runtime`.
- How an Arc application trusts the proxy: `cratis-arc-authentication-authorization-and-identity`.
