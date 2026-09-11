---
name: cratis-engineering-effect-boundaries
description: Apply the Cratis effect-boundary contract when writing or reviewing code that publishes, persists, generates, propagates, or releases. On those boundaries partial success is failure - no catch-and-continue, no defaulting to success on an unknown outcome. Use when a degraded run could still report success; defer style questions and specification authoring to their own workflows.
license: LICENSE
---
<!-- cratis-ai-managed: skills/cratis-engineering-effect-boundaries/SKILL.md -->

# Effect boundaries fail loudly

An **effect boundary** is the point where work leaves the process and becomes
something other people observe: a package published, a row written, a file
generated, content propagated to other repositories, a release cut.

The contract:

> On an effect boundary, **partial success is failure.** No catch-and-continue,
> no defaulting to success on an unknown outcome. A degraded operation must fail
> the operation, surface the delta, or emit an explicit degraded-mode signal.

Silent failure is the dominant recurring bug archetype across Cratis. The
2026-08-24 organization-wide review found one disease with six manifestations,
in the release action, the Arc proxy generator, Stage, the Chronicle container
host, Chronicle constraint enforcement, and corpus propagation. They are written
out in [failure-archetypes.md](references/failure-archetypes.md); read them
before deciding that your case is different.

## When you need this

- You are writing or reviewing a `catch` around an operation with an effect —
  publish, write, generate, copy, notify, tag, release.
- An operation processes a set and some members can fail independently: a
  fan-out, a batch, a matrix, a per-file generator.
- A call returns an outcome you did not model: an unexpected status code, a
  null, an empty result, a timeout.
- Two implementations of one interface exist and only one of them really
  enforces the behavior — an in-memory or SQL sibling of a real store.
- A host, container, or long-running process can reach a "started" state while
  the thing it started has already thrown.

## When you do not

- **Pure computation with no effect.** A parser that returns a partial tree for
  a caller that inspects it is not an effect boundary.
- **A retry that will still report the final outcome truthfully.** Retrying is
  not swallowing; reporting success after the retries also failed is.
- **A genuinely optional enrichment whose absence is stated in the result.** An
  optional cache warm that records `cache: skipped` is a degraded-mode signal,
  which is exactly what this contract asks for.
- **Style, naming, or structure questions.** Those belong to the C# and
  TypeScript conventions.
- **Deciding whether an operation should exist at all.** That is a product or
  scope ruling, not an error-handling one.

## Steps

1. **Name the boundary before you write the handler.** Say out loud what leaves
   the process: which package, which rows, which files, which repositories.
   If nothing leaves, this contract does not apply and you can stop here.
2. **Enumerate the outcomes the call can produce, including the ones you did not
   design for.** An unexpected status code, an empty response, and a timeout are
   outcomes. A handler that maps everything it did not enumerate onto success is
   the defect.
3. **Choose one of the three permitted responses to a degraded outcome, and say
   which one you chose.** Fail the operation; or complete and surface the delta
   in the result; or emit an explicit degraded-mode signal the caller must
   handle. Anything else is catch-and-continue.
4. **Make partial fan-out visible in the aggregate, not just in the log.** Count
   attempted, succeeded, and failed, and put all three in the returned result
   and the summary line. "29 of 36 succeeded" and "36 of 36 succeeded" must not
   produce the same output.
5. **Refuse to convert an unknown into a pass.** An outcome the code could not
   classify is `indeterminate`. Report it as its own state; never roll it up
   into the success count.
6. **Check the sibling implementations of the same interface.** When a real
   store enforces a constraint, its in-memory and SQL siblings must enforce the
   same one or throw `NotSupported`. A sibling that silently accepts what the
   real one rejects makes every specification that uses it pass vacuously.
7. **Make the process state follow the work.** If startup threw, the host is not
   `Running`. A liveness or readiness state that survives a failed start is a
   lie the orchestrator will believe.
8. **Plant the failure and watch it surface.** Force the degraded outcome —
   inject the status code, delete an input, fail one fan-out member — and
   confirm the operation fails, the delta appears, or the degraded signal fires.
   A boundary whose failure path was never executed is not known to have one.

## What breaks

Every item below is a real Cratis defect, not an illustration. Detail and issue
references are in [failure-archetypes.md](references/failure-archetypes.md).

- **A swallowed conflict reported as a successful release.** The release action
  caught a 422 from a concurrent publish and reported the release as done. The
  version was never published, and the only artifact that said so was a caught
  exception nobody saw.
- **A generator that degrades silently.** The Arc proxy generator emitted fewer
  proxies than its inputs implied and exited zero. The failure shows up much
  later as a missing TypeScript type, far from the generator that dropped it.
- **A renderer that quietly renders less.** Stage produced degraded output on a
  path that reported success, so the difference between correct output and
  partial output was invisible at the boundary that produced it.
- **A container that stays `Running` after startup threw.** The Chronicle host
  reported healthy while the thing it hosts had already failed to start, so the
  orchestrator kept routing to it.
- **A constraint that only one implementation enforces.** Chronicle unique
  constraints were not enforced on the SQL and in-memory storage providers.
  Every specification exercising them passed while proving nothing.
- **A fan-out that succeeded 29 times out of 36 and said "done".** Corpus
  propagation aggregated per-target results into a single success, so seven
  repositories silently did not receive the change.

The shared symptom: **the failure is discovered downstream, by someone who
cannot see the boundary that caused it.** That is what makes this archetype
expensive rather than merely annoying.

## How it is proven

- **The failure path was executed.** Name the planted defect and the observed
  result: the injected status code, the removed input, the failed fan-out
  member — and what the operation did in response.
- **Counts appear on success.** The clean run reports how many subjects it
  attempted and how many succeeded. A bare "OK" cannot be distinguished from a
  run over an empty set.
- **Exit codes carry the verdict.** `0` ran clean, `1` found defects, `2` could
  not run. A wrapper that exits `0` because the wrapper finished has thrown the
  child's verdict away; check the child's status and, in a pipeline, the status
  of every stage.
- **The degraded signal is asserted, not just emitted.** A specification reads
  the delta or the degraded-mode field and fails when it is absent.
- **Sibling implementations are covered by the same specification.** The test
  that proves the constraint runs against every implementation of the interface,
  not only the one that enforces it.
