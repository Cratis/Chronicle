# Workbench

Chronicle Workbench provides a bundled local browser surface for authorized
inspection of Chronicle runtime state and preview of supported projection
behavior.

Chronicle and its bundled local Workbench are available as MIT-licensed
self-hosted software; authorized local use is separate from paid Cratis support,
hosted coordination, or managed operational responsibility.

## Start locally

Start the reviewed Chronicle 16.38.2 development image by its multi-platform OCI
manifest digest:

```bash
docker run --rm -d --name chronicle \
  -p 27017:27017 -p 35000:35000 \
  cratis/chronicle@sha256:bc4d23570d29fdfb31a8f5ae689f6538e74ff2db7807e981438473d2283f4b07
```

Open `https://localhost:35000` in a browser and follow the current local
authentication flow presented by the development profile. The local endpoint
uses a development certificate; browser handling varies by environment.

Stop and remove the evaluation container when finished:

```bash
docker rm -f chronicle
```

## Current scope

- Workbench is bundled with Chronicle as a local browser surface.
- Access must be authorized for the exact Chronicle runtime scope.
- The surface supports runtime-state inspection and preview of supported
  projection behavior.

## Current limits

This scope does not imply complete administration, governed or production-ready
mutation, client parity, Hub/federation, managed service, support, an SLA,
security, compatibility, or production suitability.

Use the exact released Chronicle/Workbench profile and its documented
configuration when evaluating the surface. Do not infer mutation authority from
the ability to inspect runtime state.

- [Chronicle overview](/chronicle/)
- [Chronicle architecture](/chronicle/architecture/)
- [Cratis CLI](/cli/)
- [Workbench source](https://github.com/Cratis/Chronicle/tree/main/Source/Workbench)
- [Chronicle releases](https://github.com/Cratis/Chronicle/releases)
- [Chronicle license](https://github.com/Cratis/Chronicle/blob/main/LICENSE)
