---
title: Chronicle Workbench reverse proxy configuration
description: Serve the Chronicle Workbench under a reverse proxy path prefix, keep assets and live updates on that path, and avoid double-prefixing behind Stage.
---
# Workbench

Set `workbench.basePath` when a reverse proxy serves the Chronicle Workbench under a path such as `/my-instance/chronicle`. The setting keeps the page's assets and API calls under that prefix, so the browser reaches the right kernel. Leave it empty when you connect directly or give Chronicle its own host name.

Without the setting, the page requests scripts and styles from the host root. Behind a path-routing proxy, those requests can reach the wrong service and leave the Workbench blank.

## Configuration

Add the public path prefix to `chronicle.json`, without a `Cratis:Chronicle` wrapper:

```json
{
  "workbench": {
    "basePath": "/my-instance/chronicle"
  }
}
```

### Environment variable

You can set the same value through the environment instead:

```bash
Cratis__Chronicle__Workbench__BasePath=/my-instance/chronicle
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| workbench.basePath | string | empty | The path the Workbench is served under. Empty serves it at the root of the host. |

A leading slash is added and a trailing slash dropped, so `my-instance/chronicle/` and `/my-instance/chronicle` are the same setting.

## What it changes

With a base path set, the kernel:

- writes the path into the `base-path` meta tag of the Workbench page, which the Workbench uses for routing and as the base of every API call, event stream and WebSocket;
- prefixes the root-absolute `src` and `href` references of the page; and
- accepts requests with or without the prefix, so it does not matter whether your proxy strips the prefix before forwarding or passes it through.

The prefix does not restrict access to unprefixed routes: the kernel accepts both forms. gRPC clients keep their existing connection address.

For this example, route `https://admin.example.com/my-instance/chronicle/` to the kernel's main port. Open that URL and inspect the browser's network requests: scripts, styles and API calls should all use `/my-instance/chronicle/`. If the page loads but live data does not, check the proxy's streaming and WebSocket settings below.

## Reverse proxy examples

Use the same `basePath` whether the proxy passes the prefix through or strips it. The examples below keep TLS enabled on the kernel; configure the proxy to trust the backend certificate. See [TLS configuration](tls.md) for certificate setup. The path prefix is for Workbench browser traffic, not the gRPC client connection.

### Traefik

Pass the prefix through:

```yaml
labels:
  - traefik.enable=true
  - traefik.http.routers.chronicle.rule=PathPrefix(`/my-instance/chronicle`)
  - traefik.http.services.chronicle.loadbalancer.server.port=35000
  - traefik.http.services.chronicle.loadbalancer.server.scheme=https
```

To strip the prefix before forwarding instead, add this `stripprefix` middleware to the router. Keep the same Workbench `basePath`:

```yaml
  - traefik.http.routers.chronicle.middlewares=chronicle-strip
  - traefik.http.middlewares.chronicle-strip.stripprefix.prefixes=/my-instance/chronicle
```

### nginx

A `proxy_pass` without a path passes the prefix through. Adding a trailing slash strips it:

```nginx
# Prefix passed through
location /my-instance/chronicle/ {
    proxy_pass https://chronicle:35000;
}

# Prefix stripped
location /my-instance/chronicle/ {
    proxy_pass https://chronicle:35000/;
}
```

Chronicle sends live updates to the Workbench over server-sent events (SSE) and WebSockets. Keep response buffering off so SSE updates arrive as they happen, and allow connection upgrades for WebSockets. In nginx, configure `proxy_buffering off;`, `proxy_http_version 1.1;` and the `Upgrade` and `Connection` headers in the chosen location block. The path-routing examples above do not include those streaming settings.

### Try it

To compare both routing styles locally, use the [WorkbenchBehindProxy sample](https://github.com/Cratis/Chronicle/tree/main/Samples/WorkbenchBehindProxy). It runs two instances behind Traefik with `docker compose up`, one passing the prefix through and one stripping it.

## Why a setting and not a forwarded header

The kernel does not read `X-Forwarded-Prefix`; set `workbench.basePath` explicitly in the instance's deployment configuration. Its forwarded-header policy accepts the client IP and public scheme from loopback and the proxies listed in `forwardedHeaders`. That policy does not supply a Workbench path prefix.

## Behind Cratis Stage

Leave `workbench.basePath` empty for the kernel bundled with a Cratis Stage play session. Stage serves that kernel's Workbench under `/workbench` and rewrites the page as it proxies the response. It resolves the public prefix on each request, including a prefix supplied by an upstream proxy through `X-Forwarded-Prefix`.

Stage needs the kernel to serve the page with its original root-relative asset paths and empty `base-path` meta tag. Setting the kernel's base path as well would prefix the assets twice.
