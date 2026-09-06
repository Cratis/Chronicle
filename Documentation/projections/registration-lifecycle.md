# Registration lifecycle

Clients register their projections on every connect — the full set of definitions the client's code
declares. Chronicle compares each incoming definition against the registered one and only acts on
what changed, so a re-registration from an unchanged client is near-free.

## When a definition changes

A changed definition — including one that *removes* a child collection — is stored, pushed to the
engine on every silo, and the projection's observer keeps running against the new definition. The
change also raises a **replay recommendation** ("Projection definition has changed"), because read
models already written with the old definition may no longer match; performing the recommendation
rebuilds them. With `ReplayOnDefinitionChange` enabled, the replay happens automatically instead.

## When a registration partially fails

Definitions are isolated from each other: a definition the engine rejects fails *alone*. Every other
definition in the registration still lands — in the engine, in its projection grain, and in the
registered state — and the failure is reported back to the client naming exactly the projections that
did not register. A failed definition is not marked registered, so the next registration retries it.

## When the kernel is busy

Registering a projection against a store that already holds events starts a catch-up, and registration
does not wait for it. A projection catches up through a single job step that walks the sequence in
global order, and that step is brought up on the observer's own turn rather than inside the
registration call — so the size of the event log does not decide how long registering takes.

Registration is also retried rather than fatal. The client re-runs the whole registration with an
exponential backoff before giving up, configurable through
[`RegistrationRetry`](../configuration/chronicle-options) — every step is idempotent, so a retry
costs the kernel only the comparison it already does. A host therefore waits out a kernel that is
merely busy instead of failing to start, restarting, and re-registering into the queue it was waiting
on. A kernel that is genuinely refusing the definitions still fails the host, once the attempts are
spent.

## When a projection is no longer registered — retirement

A projection the client stops declaring — its read model was deleted, or renamed and thereby given a
new identity — is **retired** when the client next registers its full set:

- Its observer is unsubscribed in every namespace, so it stops consuming events.
- Its jobs are deleted and its failed partitions are cleared.
- Its definition is removed from the engine and from storage.
- Its **sink container is left untouched** — data is never deleted implicitly. Drop or clean the
  container yourself if the data is no longer wanted.

Retirement only happens for a **full-set** registration: the client SDK marks the registration as the
complete set for its owner, and only when every discovered artifact produced a definition. Partial
registrations — such as saving a single projection from the Workbench — never retire anything, and a
client-owned full set never retires server-owned projections (or the other way around).

### Renamed read models and shared containers

A renamed read model keeps its type name and thus its **container name**. Without retirement, the old
projection and its successor would keep writing the same container with different definitions and
overwrite each other's documents. With retirement the old projection stops, and because another
registered projection targets the same container, Chronicle raises a replay recommendation for the
successor so the container can be rebuilt cleanly from its definition alone.
