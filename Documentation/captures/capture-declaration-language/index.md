# Capture Declaration Language

The Capture Declaration Language (CDL) is an indentation-based DSL for defining captures that transform external data changes into Chronicle events.

## Overview

CDL definitions compile to `CaptureDefinition` and support:

- Source declarations (`api`, `webhook`, `message`)
- Key declaration for identity and diffing
- Optional map operations (`translate`, `split`, field rename, template assignment)
- Event append rules with `when` conditions
- Nested object scopes
- Child collection scopes

## Example

```cdl
capture InvoiceCapture
  source api
    api InvoicingApi
    route /invoices
    poll 10m
  key id
  map
    status = status translate
      "utkast" => draft
      "betalt" => paid
  append InvoiceStatusChanged
    when status
    status = $.status
    changedAt = $context.occurred
  nested billingAddress
    append InvoiceBillingAddressChanged
      when street or city
      street = $.billingAddress.street
  children lineItems identified by lineNumber
    append InvoiceLineItemAdded
      when added
      lineNumber = $.lineNumber
    append InvoiceLineItemRemoved
      when removed
      lineNumber = $.lineNumber
```

## Language elements

### Header

- `capture <Name>` defines one capture.

### Source block

```cdl
source api|webhook|message
  ...
```

Source properties:

- API: `api`, `route`, `poll`
- Webhook: `path`
- Message: `topic`

For API sources, `api` identifies a configured **[External Service](../../external-services/index.md)** by name. The External Service holds the base URL and the authentication for the connection. `route` is optional and is appended to that base URL; if omitted, the base URL is used as-is.

> [!NOTE]
> Authentication is **not** part of the CDL. For API sources it is configured on the referenced External Service; for webhook sources it is configured in code on the source builder. Either way, secrets and tokens never live in capture text. See [Configuring authentication](#configuring-authentication) below.

### Configuring authentication

**API sources** connect through a configured External Service. Configure the base URL and authentication once on the External Service; the capture simply references it by name:

```csharp
builder.FromApi("InvoicingApi", source => source
    .OnRoute("/invoices")
    .PollEvery("10m"));
```

**Webhook sources** are inbound and configure their authentication in code on the source builder:

- `WithBasicAuth(username, password)` — basic authentication
- `WithBearerToken(token)` — bearer token authentication
- `WithOAuth(authority, clientId, clientSecret)` — OAuth authentication

When no authentication is configured, the source is treated as unauthenticated.

### Key directive

- `key <propertyPath>`

### Map block

`map` supports:

- field rename: `target = source`
- template assignment: ``target = `template ${expr}```
- translate: `target = source translate` + value entries
- split:

  ```cdl
  split source by ","
    first
    second
  ```

### Append block

```cdl
append <EventType>
  when ...
  <targetField> = <sourceExpression>
```

Supported `when` forms:

- `when property`
- `when p1 or p2`
- `when p1 and p2`
- `when property from old to new`
- `when added`
- `when removed`
- `when \`expr\``

### Nested block

```cdl
nested <objectPath>
  [map ...]
  append ...
```

### Children block

```cdl
children <collectionPath> identified by <childKey>
  [map ...]
  append ...
```

## Expressions

Typical source expressions:

- `$.path` (current payload)
- `$previous.path` (previous payload)
- `$context.occurred` (capture context)
- `$env.VARIABLE` (environment lookup)

## Formal language specification

See [Grammar (EBNF)](grammar.md) for the full formal syntax.
