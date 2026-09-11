<!-- cratis-ai-managed: skills/cratis-engineering-effect-boundaries/references/failure-archetypes.md -->
# The six cited failure archetypes

These are the six manifestations the 2026-08-24 Cratis organization-wide review
identified as one disease: silent failure on an effect boundary. Each is a real,
tracked defect. Read them as the shape of the mistake, not as a list of fixed
bugs — the same shape keeps reappearing in new code.

For each: what the boundary was, what the code did, why the failure was
expensive, and what the contract required instead.

## 1. A swallowed conflict reported as a successful release

*release-action #178.*

**Boundary.** Publishing a version — the most public effect there is.

**What happened.** A concurrent publish made the registry return HTTP 422. The
action caught it and continued, and the run reported the release as successful.

**Why it was expensive.** The one artifact that recorded the truth was an
exception nobody saw. Everything downstream — release notes, subscriber pins,
the assumption that the version existed — was built on a success that had not
happened. Nothing later in the pipeline re-checked the registry, because a
successful publish is normally proof enough.

**What the contract required.** A 422 on publish is an outcome that must be
classified, not caught. Either the version already exists and is byte-identical
(report it as already-published, explicitly), or it does not and the publish
failed. "Caught an exception, carried on" is neither.

## 2. A generator that degrades silently

*Arc #2571, #2564, #2527 — the proxy generator.*

**Boundary.** Generating TypeScript proxies from C# sources. The output is what
the whole frontend compiles against.

**What happened.** The generator produced fewer proxies than its inputs implied
and still exited zero.

**Why it was expensive.** The symptom appears far from the cause: a missing
TypeScript type in a component, at a point where nobody is thinking about the
generator. The natural first hypothesis is that the frontend is wrong.

**What the contract required.** A generator that consumed N inputs and emitted
fewer than N artifacts reports the delta and fails, or names the skipped inputs
and why. Reporting the count on success is what makes the shortfall visible at
all: "generated 41 of 41" and "generated 38 of 41" have to read differently.

## 3. A renderer that quietly renders less

*Stage #53.*

**Boundary.** Rendering output that someone will look at and act on.

**What happened.** A degraded rendering path produced partial output while
reporting success.

**Why it was expensive.** Partial output looks like output. There is no error to
search for and no count to compare, so the difference between correct and
degraded is invisible at exactly the boundary that produced it.

**What the contract required.** A renderer that could not render something says
so in its result — a degraded-mode signal the caller has to handle, not a log
line at the end of a stream nobody reads.

## 4. A container that stays `Running` after startup threw

*Chronicle #3682.*

**Boundary.** Process and container lifecycle — the state an orchestrator reads
to decide whether to send traffic.

**What happened.** Startup threw, and the container remained in `Running`.

**Why it was expensive.** The orchestrator believed the reported state and kept
routing to a host that had never finished starting. The failure surfaces as
inexplicable behavior in callers rather than as a failed start.

**What the contract required.** Process state follows the work. If startup
failed, the process exits non-zero or reports unhealthy. A liveness state that
survives a failed start is not a degraded signal, it is a false one.

## 5. A constraint that only one implementation enforces

*Chronicle #3744 — unique constraints on the SQL and in-memory providers.*

**Boundary.** Persistence, and the invariant the store is supposed to guarantee.

**What happened.** Unique constraints were not enforced on the SQL and in-memory
storage providers, while the primary provider enforced them.

**Why it was expensive.** This is the worst variant, because it does not fail —
it makes specifications pass vacuously. Every test written against the
in-memory provider proved that duplicate writes were accepted, and read as
proof that the constraint worked. The gap only appears in the one environment
nobody tests against by default.

**What the contract required.** An alternate implementation of an interface
matches the primary one's *semantics*, not just its signature. Where it cannot,
it throws rather than silently accepting. The specification that proves the
constraint runs against every implementation.

## 6. A fan-out that succeeded 29 times out of 36 and said "done"

*The retired corpus propagation.*

**Boundary.** Propagating content into other repositories — an effect in 36
places at once.

**What happened.** Per-target results were aggregated into a single overall
success. Seven repositories did not receive the change, and the aggregate said
nothing about it.

**Why it was expensive.** Nobody knew which seven. Recovering meant re-deriving
the target list and comparing every repository by hand, long after the run's
own record of what happened had been lost to log rotation.

**What the contract required.** Attempted, succeeded, and failed are three
separate numbers, all three in the returned result and the summary line. A
fan-out that cannot name its failures has not reported its outcome. "29 of 36"
is not a success with a footnote; on an effect boundary it is a failure.

## What the six have in common

- The failing code **caught something and continued**, or **mapped an
  unmodelled outcome onto success**.
- The success signal was **produced by the layer that failed**, so no later
  check re-derived it.
- The cost was paid **downstream, by someone who could not see the boundary**.
- In the two worst cases (2 and 5) the defect made verification itself
  meaningless: a green generator run and a green specification suite that were
  both measuring nothing.
