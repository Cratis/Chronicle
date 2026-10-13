# Workbench behind a path-routed reverse proxy

Runs two Chronicle instances behind one Traefik host, each Workbench under its own path. It shows both kinds of proxy: one that passes the prefix through and one that strips it.

```bash
docker compose up
```

Then open:

- <http://localhost:8080/alpha/chronicle/> (prefix passed through)
- <http://localhost:8080/beta/chronicle/> (prefix stripped by Traefik)

Sign in with the development credentials from the [Workbench documentation](https://github.com/Cratis/Chronicle/blob/main/Documentation/hosting/configuration/workbench.md). The kernel keeps TLS on, so Traefik is told to trust its self-signed certificate. That is for this local sample only. The only Chronicle setting involved is `Cratis__Chronicle__Workbench__BasePath`.

gRPC clients do not use the prefix. They connect to the instance directly, for example `chronicle://localhost:35000`, so publish the kernel port as well if you want to connect a client to one of these.
