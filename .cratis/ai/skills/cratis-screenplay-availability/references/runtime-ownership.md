<!-- cratis-ai-managed: skills/cratis-screenplay-availability/references/runtime-ownership.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Who owns what at runtime

The model says what is gated and what is provided; the runtime enforces it. A model author never
writes enforcement code and an application never calls the pieces below.

| Piece | Owns |
| --- | --- |
| Screenplay | The language: `audience`, `availability`, `available`, `provide`, their diagnostics, and the reference evaluator (every gate available in specifications; opaque sources unsupported) |
| Choreography engine | Evaluates an availability for a request: Available, Unavailable with a reason, or Unknown, which denies. A failure to decide is an error result, never a denial. Request-scoped context values are resolved lazily, once per request |
| Showrunner GuestList | The audience contracts: public events that grant and withdraw an audience at tenant, user and system level, payload field `Audience`; the newest grant wins and a withdrawal removes only its own level |
| Arc | Enforcement at the command and query boundary, before authorization, refusing with `NotAvailable` |
| Stage | Renders and runs the admitted model with the engine; consumes it rather than owning it |
| Direct | Not called by an application. Box Office module entitlements stay separate and unchanged |

## Consequences for modeling

- The model declares audiences and never members. Do not add a membership event or a grant command to the
  model to simulate GuestList; granting is the runtime's job.
- Chronicle's routing audience stays in event options and is never a field of an event.
- A union of audiences comes from tenant, user and system levels; a reaction sees the tenant's and system's only.
- Decide a hidden entry from `NotAvailable`, not from guessing the package in client code.
- An `available when` reading a provided value depends on that value resolving per request: choose a key that
  exists for the job (`$context.command.<key>` or `$context.arguments.<key>`).
