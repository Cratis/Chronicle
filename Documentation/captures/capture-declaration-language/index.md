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
    auth bearer $env.API_TOKEN
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

```
source api|webhook|message
  ...
```

Source properties:

- API: `api`, `route`, `poll`, `auth`
- Webhook: `path`, `auth`
- Message: `topic`

For API sources, `api` identifies the configured API definition. `route` is optional; if omitted, the base API URL is used as-is.

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

```
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

```
nested <objectPath>
  [map ...]
  append ...
```

### Children block

```
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
