# Workbench

The kernel serves the Workbench on the main port, at the root of the host. That is all it needs when you reach Chronicle directly, or through a proxy that gives it a host of its own. When a reverse proxy routes by path instead — one shared host, one path per instance — the root-absolute scripts, styles and API calls of the page point at the wrong place and the page stays blank. The `workbench.basePath` setting fixes that.

## Configuration

```json
{
  "workbench": {
    "basePath": "/my-instance/chronicle"
  }
}
```

### Environment variable

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

Everything else is untouched. gRPC clients connect to the same address as before.

For the example above, the Workbench is served at `https://admin.example.com/my-instance/chronicle/`, and the proxy only needs to route that path to the kernel's main port.

## Reverse proxy examples

The setting is the same whichever way the proxy treats the prefix. Keep TLS enabled on the kernel, and let the proxy either trust its certificate or validate it, as you would for any backend. Only the Workbench is path-routed; gRPC clients connect to the kernel directly and never use the prefix.

### Traefik

Pass the prefix through:

```yaml
labels:
  - traefik.enable=true
  - traefik.http.routers.chronicle.rule=PathPrefix(`/my-instance/chronicle`)
  - traefik.http.services.chronicle.loadbalancer.server.port=35000
  - traefik.http.services.chronicle.loadbalancer.server.scheme=https
```

Or strip it first, by adding a `stripprefix` middleware to the router. Chronicle behaves the same:

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

Chronicle streams live updates to the Workbench over server-sent events and WebSockets, so keep response buffering off and allow connection upgrades. In nginx that is `proxy_buffering off;`, `proxy_http_version 1.1;` and the `Upgrade` and `Connection` headers.

### Try it

The [WorkbenchBehindProxy sample](https://github.com/Cratis/Chronicle/tree/main/Samples/WorkbenchBehindProxy) runs two instances behind Traefik, one for each style, with `docker compose up`.

## Why a setting and not a forwarded header

Chronicle does not read `X-Forwarded-Prefix`. The kernel trusts forwarded headers from loopback and from the proxies you list in `forwardedHeaders`, and a prefix taken from a header would let anything that can reach the kernel rewrite the page it serves. An explicit setting is also what a deployment on Kubernetes already has at hand: it is configuration of that instance.

## Behind Cratis Stage

A Cratis Stage play session serves the Workbench of its own bundled kernel under `/workbench`, and works out the prefix itself on every request, including the one a proxy in front of the Stage states in `X-Forwarded-Prefix`. It rewrites the page on the way through. Leave `workbench.basePath` empty for that kernel: with it empty the page is served exactly as it ships, which is what the Stage rewrites. Setting both would prefix the page twice.
