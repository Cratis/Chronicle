<!-- cratis-ai-managed: skills/cratis-arc-configuration/references/frontend-and-routes.md -->
# Frontend and routes

## Contents

- Settings that must match
- React provider props
- Hosting under a path prefix

## Settings that must match

The route settings exist twice. The proxy generator reads MSBuild properties and
writes URLs into the generated client; the host reads `Cratis:Arc:GeneratedApis`
and maps endpoints. Arc does not compare them. A mismatch gives a 404 with no
build error and no log entry.

| MSBuild property (generator) | Host key `Cratis:Arc:GeneratedApis:...` | Generator default | Host default |
| --- | --- | --- | --- |
| `CratisProxiesApiPrefix` | `RoutePrefix` | `api` | `api` |
| `CratisProxiesSegmentsToSkip` | `SegmentsToSkipForRoute` | none | `0` |
| `CratisProxiesSkipCommandNameInRoute` | `IncludeCommandNameInRoute` (inverse) | `false` | `true` |
| `CratisProxiesSkipQueryNameInRoute` | `IncludeQueryNameInRoute` (inverse) | `false` | `true` |

Change both sides in one commit, then compare a generated client's `route` with
the mapped endpoint. An explicit `[Path]` on a model-bound artifact replaces the
route and does not get the prefix. The full property list is in the proxy
generator reference of `cratis-arc-command`.

## React provider props

Props on `<Arc>`. Inner providers do not inherit outer values.

| Prop | Default | Change it when |
| --- | --- | --- |
| `apiBasePath` | `''` | The application is served under a path prefix. Never repeat the generated route's `/api`. |
| `origin` | `''` (current origin) | The backend is on another origin. Requests then bypass a dev-server proxy. |
| `microservice` | `''` | An ingress selects a service through a header. |
| `development` | `false` | Turn on development behavior in the provider. |
| `httpHeadersCallback` | none | Dynamic headers on fetch requests; not applied to native streaming handshakes. |
| `queryTransportMethod` | `ServerSentEvents` | Another observable-query transport. |
| `queryDirectMode` | `false` | `true` skips shared hubs. |
| `queryConnectionCount` | `1` | More hub connection slots (server-sent events capped at four). |
| `observableQueryTransferMode` | `Delta` | Full snapshots instead of deltas. |
| `queryCacheRetentionMs` | `30000` | Cache lifetime after the last consumer releases an entry. |
| `basePath` | `''` | Deprecated; does nothing. |

## Hosting under a path prefix

For an app served at `/workbench`: set `<Arc apiBasePath="/workbench">`, set the
router `basename` to `/workbench` separately, and make the backend accept the
prefix (`UsePathBase` for ASP.NET Core; static file request path for the
lightweight host). `/workbench` plus the generated `/api/orders` becomes
`/workbench/api/orders`. A reverse proxy that strips the prefix means the
server sees unprefixed paths; configure the frontend and ingress together.
