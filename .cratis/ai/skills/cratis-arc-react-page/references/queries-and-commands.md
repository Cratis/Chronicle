<!-- cratis-ai-managed: skills/cratis-arc-react-page/references/queries-and-commands.md -->
# Queries and commands reference

A Debug build generates one typed proxy class per query and per command. The
proxy exposes static React hooks; the page calls those. Never edit a generated
file — fix the C# source and rebuild.

## Query proxy hooks

| Static | When |
| --- | --- |
| `<Query>.use(args?, sorting?)` | snapshot query, re-renders on change |
| `<Query>.useWithPaging(pageSize, args?, sorting?)` | server-side paging |
| `<Query>.useSuspense(...)` | suspense-aware; must render inside a query boundary |
| `<Query>.useSuspenseWithPaging(pageSize, ...)` | suspense plus paging |
| `<ObservableQuery>.use(...)` | live query pushed over the query hub |
| `<ObservableQuery>.useChangeStream(args?, getKey?, sorting?)` | item-level deltas |
| `<Query>.when(condition).use(args)` | conditional — never wrap a hook in `if` |

### Return tuples

These shapes are load-bearing. The observable variants have **no perform
function**:

| Hook | Returns |
| --- | --- |
| `useQuery(query, args?, sorting?, isEnabled?)` | `[result, perform, setSorting]` |
| `useQueryWithPaging(query, paging, args?, sorting?, isEnabled?)` | `[result, perform, setSorting, setPage, setPageSize]` |
| `useSuspenseQuery(...)` | `[result, perform, setSorting]` |
| `useSuspenseQueryWithPaging(...)` | `[result, perform, setSorting, setPage, setPageSize]` |
| `useObservableQuery(query, args?, sorting?, isEnabled?)` | `[result, setSorting]` |
| `useObservableQueryWithPaging(query, paging, ...)` | `[result, setSorting, setPage, setPageSize]` |
| `useChangeStream(query, args?, getKey?, sorting?, isEnabled?)` | `ChangeSet<T>` |

`setSorting`, `setPage`, and `setPageSize` all return a promise.

The underlying hooks live in `@cratis/arc.react/queries` if you need them
directly.

### The result object

Every query hook returns a result carrying:

`data`, `paging`, `isSuccess`, `isAuthorized`, `isValid`, `isPerforming`,
`isReady`, `hasData`, `hasExceptions`, `validationResults`,
`exceptionMessages`, `exceptionStackTrace`, `changeSet`.

Guard rendering with `hasData` rather than checking `data.length` — a
conditional query that has not fired returns an empty result with
`hasData: false`.

### Paging

```ts
class Paging { constructor(page?: number, pageSize?: number); page: number; pageSize: number; get hasPaging(): boolean }
class Sorting { constructor(field: string, direction: SortDirection); get hasSorting(): boolean }
enum SortDirection { unspecified = 0, ascending = 1, descending = 2 }
```

The result's paging block is a different shape from the request's — note
`size`, not `pageSize`:

```ts
class PagingInfo { page: number; size: number; totalItems: number; totalPages: number }
```

`page` is zero-based. `totalItems` is the total count. A read model returning a
queryable gets server-side paging and sorting for free — use it whenever a list
can grow. Over the wire the parameters are `page`, `pageSize`, `sortBy`, and
`sortDirection`.

### Change streams

```ts
class ChangeSet<T> { readonly added: T[]; readonly replaced: T[]; readonly removed: T[] }
```

Without `getKey` removed items cannot be identified and everything arrives as
`added`; `getKey` is what enables `replaced` and `removed` detection. Change
streams exist only on observable queries returning a collection.

### Suspense

`useSuspense` throws a promise while loading and a typed error on failure, so it
must render inside `QueryBoundary` (`@cratis/arc.react/queries`), which combines
suspense with the query error boundary. It re-suspends on refresh, sorting, and
paging changes because the cache entry is cleared. In test teardown call
`clearSuspenseQueryCache()` and `clearSuspenseObservableQueryCache()`.

`QueryFailed` and `QueryUnauthorized` are the thrown error types; `QueryFailed`
carries `exceptionMessages` for logging.

### Query scope

`QueryScope` plus `useQueryScope()` aggregates one `isPerforming` flag across
the queries rendered inside it. Members register automatically and scopes nest,
with an inner scope reporting to the nearest outer one.

## Observable query transport

The `<Arc>` provider defaults `queryTransportMethod` to server-sent events;
the library-level default without the provider is WebSocket. Both run through a
multiplexed hub by default (`queryDirectMode` is `false`), so many observable
queries share one connection. Server-sent-event hub connections are capped —
raising `queryConnectionCount` above four warns, because HTTP/1.1 limits
concurrent SSE streams.

`observableQueryTransferMode` defaults to delta: the server sends a change set
and the client reconstructs the collection from the previously cached result.
Set it to full to receive every item as `added` on each push.

Identical subscriptions share one connection through the query instance cache,
which retains an unsubscribed entry briefly (30 seconds by default) so a
remount does not re-subscribe. After login or logout call `reconnectQueries()`
from the Arc context to force re-subscription with the new identity.

## Command proxy

```tsx
const [command, setCommandValues, clearCommandValues] = RegisterAccount.use();
command.name = 'Acme';                     // or setCommandValues({ name: 'Acme' })
const result = await command.execute();
```

`use(initialValues?)` returns a three-element tuple. The instance also exposes
`validate()`, `clear()`, `setInitialValues(values)`, `revertChanges()`, and
`hasChanges`.

### Read the result by the granular flag

| Flag | Meaning | Response |
| --- | --- | --- |
| `isSuccess` | authorized, valid, and no exception | happy path |
| `isAuthorized` | roles or policy rejected the call | redirect to login, or "not allowed" |
| `isValid` | validation failed — `validationResults` carries the messages | render inline field errors |
| `hasExceptions` | the handler threw — `exceptionMessages` carries diagnostics | generic error, and log it |

The result also carries `correlationId`, `authorizationFailureReason`,
`exceptionStackTrace`, and the typed `response`. Fluent callbacks
`onSuccess`, `onFailed`, `onException`, `onUnauthorized`, and
`onValidationFailure` each return the result for chaining.

```tsx
const result = await command.execute();
if (!result.isAuthorized) { redirectToLogin(); return; }
if (!result.isValid) { /* validationResults -> inline field errors */ return; }
if (result.hasExceptions) { console.error(result.exceptionMessages); return; }
```

A validation result is:

```ts
class ValidationResult {
    severity: ValidationResultSeverity;   // Unknown 0, Information 1, Warning 2, Error 3
    message: string;
    members: string[];                    // the properties the message applies to
    state: unknown;
    reason: ValidationResultReason;       // 'rule' by default
    reasonDetail?: string;
}
```

Render per-field errors from `members` and `message`. Never branch on the raw
message text. `exceptionMessages` and `exceptionStackTrace` are for logging,
never for users.

`toastCommandResult(result, options)` from `@cratis/components/Notifications`
collapses the whole branch into one call — mount a `<Toaster />` near the app
root and it maps every flag to the right toast without showing stack traces. It
returns `true` when the command succeeded:

```tsx
if (toastCommandResult(result, { successTitle: 'Account registered' })) refresh();
```

For ad-hoc notifications the imperative `toast.success/info/warn/error` each
take an **options object**, not a bare string:
`toast.info({ title: 'Saved', description: 'Your changes were saved.' })`.

`CommandDialog` performs all of this itself — the flags matter most when
executing a command outside a dialog.

### Command scope

`CommandScope` plus `useCommandScope()` (`@cratis/arc.react/commands`)
aggregates change and execution state across several commands on one screen.
Members register automatically, scopes nest, and the scope is injectable into a
view model as `ICommandScope`. It exposes `hasChanges`, `isPerforming`,
`hasValidationFailures`, `hasExceptions`, per-command and aggregated failures,
`execute()` (which runs only the commands that have changes), and
`revertChanges()`.

## `<Arc>` configuration

The top-level provider configures the microservice name, API base path, and
query transport. Its props include `microservice`, `origin`, `basePath`,
`apiBasePath`, `httpHeadersCallback` (merged into every request — bearer and
tenant headers), `detailsType`, `queryTransportMethod`, `queryConnectionCount`,
`queryDirectMode`, `observableQueryTransferMode`, and `queryCacheRetentionMs`.
`QueryTransportMethod` and `ObservableQueryTransferMode` import from
`@cratis/arc`; `Arc` imports from `@cratis/arc.react`.
