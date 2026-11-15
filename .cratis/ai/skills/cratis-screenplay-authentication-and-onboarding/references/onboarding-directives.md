<!-- cratis-ai-managed: skills/cratis-screenplay-authentication-and-onboarding/references/onboarding-directives.md -->
# Onboarding directives

A complete model: personas are declared before the blocks name them.

```text
policy IsOwner
  require role "Owner"

policy IsAdministrator
  require role "Administrator"

policy IsAccountant
  require role "Accountant"

policy IsInvoiceManager
  require role "InvoiceManager"

persona Owner
  description "Owns the tenant"
  goal "Run the tenant"
  policy IsOwner

persona Administrator
  description "Administers the tenant"
  goal "Keep people onboarded"
  policy IsAdministrator

persona Accountant
  description "Keeps the books"
  goal "Keep the books correct"
  policy IsAccountant

persona InvoiceManager
  description "Manages invoices"
  goal "Keep invoices moving"
  policy IsInvoiceManager

authentication
  provider EntraId
  provider GitHub
  provider OpenId name Partner
  provider OpenId name Supplier

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

Syntax and meaning of each line of the `onboarding` block. All lines are optional and each may appear once.

| Directive | Form | Meaning |
|---|---|---|
| `signup` | `signup open creates tenant` or `signup open joins tenant` | Anyone may sign up; signing up creates a tenant or asks to join one. Absent: signup is off. |
| `invite tenant` | `invite tenant as <Persona>` | An invitation that creates a tenant; the invitee becomes the persona in it. One line, no body. |
| `invite member` | body with `by <Persona>, ...` and `grant <Persona>, ...` | An invitation into an existing tenant. `by` lists who may send it, `grant` what the invitee is given. Both are required. |
| `legal terms` | body of `file <path>` lines | Documents a newcomer must accept. Present means acceptance is required. |
| `offering` | `offering <Id>` | The Box Office offer a new tenant starts on. Absent means no licensing. |
| `emails` | body of `<email> file <path>` lines and `images <folder>` | Templates for `invitation`, `welcome` and `signupDeclined`, and the folder they take images from. |

## What the block deliberately lacks

No sender address, mail server, credentials or price. Those are environment values (see the key
table in `SKILL.md`). Languages for the emails follow the cultures of the model's `.strings` files.

## Diagnostics

| Code | Meaning | Fix |
|---|---|---|
| `PLAY0826` | The `onboarding` line carries more than the keyword. | Move content into the body. |
| `PLAY0827` | More than one `onboarding` block. | Merge them. |
| `PLAY0828` | `onboarding` without `authentication`. | Declare the providers. Draft authoring reports a warning until the folder is assembled. |
| `PLAY0829` | A line is not a directive, or one is malformed or incomplete. | Use the forms above. |
| `PLAY0830` | A directive stated twice. | Keep one. |
| `PLAY0831` | An invitation names a persona nothing declares. | Declare it with `persona`. |
| `PLAY0832` | `emails` names an email onboarding does not send. | Use `invitation`, `welcome` or `signupDeclined`. |

A missing `file` target is reported like a missing handler file: an attachment warning when files are loaded.
