<!-- cratis-ai-managed: skills/cratis-arc-react-page/references/availability.md -->
# Availability presentation

Use `@cratis/arc.react/availability` to offer navigation, screens or actions only
when that part is available to the current tenant or user. This is presentation
only, never authorization or a replacement for server enforcement. Availability
decides what is offered; authorization decides what the caller may do. The
server remains authoritative for every command and query.

## Provider and states

The subpath exports `AvailabilityContext`, `AvailabilityProvider`,
`RequireAvailable`, `useAvailability`, `useAvailableItems`,
`AvailabilityProviderProps` and `RequireAvailableProps`.
Mount `AvailabilityProvider` below `Arc` when the host opts
in. It observes the server's unavailable-parts query and updates live.

Keep these states distinct:

| State | Presentation |
| --- | --- |
| Not opted in: no provider | the default context is `Availability.everything`; everything is offered |
| Unknown: provider mounted, no successful decision yet | `isKnown` is false; no part is reported available; show loading, not a denial |
| Known decision | parts not listed as unavailable are available |
| Later query error | keep the last successful decision; an error is not a denial |

If the first query fails, availability stays unknown. The hook does not expose
an error state or error slot: unknown, error and not opted in are not synonyms.
Do not turn an observation failure into a permanent denial or blanket login
redirect.

## Gate a screen or part

```tsx
import { AvailabilityProvider, RequireAvailable } from '@cratis/arc.react/availability';
import { AllAccounts } from './AllAccounts';

// Render below Arc; AccountsPage, Loading and NotAvailable are application UI.
<AvailabilityProvider>
    <RequireAvailable part={AllAccounts} whileLoading={<Loading />} unavailable={<NotAvailable />}>
        <AccountsPage />
    </RequireAvailable>
</AvailabilityProvider>
```

`RequireAvailable` takes `part` and `children`; optional `whileLoading` and
`unavailable` both default to nothing. `part` is an `AvailabilityPart` from
`@cratis/arc/availability`: a full server command/read-model name, or a generated
proxy class/instance. Strings match the full name; proxies match their slice
location. Missing slice metadata is not an authorization grant.

## Navigation and decisions in code

`useAvailability()` returns the `Availability` decision: `isKnown`,
`isAvailable(part)` and `filter(items, partOf)`. `useAvailableItems(items, partOf)`
filters navigation entries live; `partOf(item)` returns a part or `undefined`.
An entry without a part is always kept, including while the decision is unknown.
Keep the entries and selector stable (outside render or memoized).

For command/query refusal feedback, use `isNotAvailable(result)` from
`@cratis/arc/availability` to distinguish unavailable from forbidden. It checks
unauthorized plus `authorizationFailureReason === 'NotAvailable'`. A temporary
`isRetryable` outcome is not a permanent denial. Read the owning
[CommandResult reference](../../cratis-arc-command/references/command-result.md)
for the result decision table and safe feedback; hiding a control does not
make execution authorized or guaranteed to succeed.
