---
applyTo: "**/*.go,go.mod,go.sum,**/go.mod,**/go.sum"
paths:
  - "**/*.go"
  - "go.mod"
  - "go.sum"
  - "**/go.mod"
  - "**/go.sum"
---
<!-- cratis-ai-managed: harnesses/copilot/instructions/go.instructions.md -->
<!-- cratis-ai: generated Copilot instructions. Do not edit; rules/ is the source. -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Go conventions

Use Go's own packages, constructors, explicit registration and standard `testing`.
Do not import C# namespace layouts, one-type-per-file rules, `I` interface prefixes,
`Async` suffixes or .NET specification mechanics. Product APIs belong in the
matching Go skill; repository-owned context supplies module paths, layout and
exact commands.

## Minimum version and dependencies

- Read the `go` directive, build constraints and CI before choosing a feature.
  The minimum is not the newest locally installed compiler; a `toolchain`
  preference does not replace minimum-version testing. Do not raise it incidentally
  or rewrite unrelated code to modernize syntax. Verify unfamiliar APIs against the
  documentation for that minimum, and keep experimental features opt-in.
- Iterators require Go 1.23; `T.Context` and `B.Loop` require 1.24;
  stable `testing/synctest` and `WaitGroup.Go` require 1.25.
- Prefer the standard library when it meets the contract. Review dependency
  capability, license and compatibility before adding one.
- Change module files only for authorized dependency work. Review tidy changes,
  including indirect minimum-version changes; keep sums consistent. A consumer
  must not need a developer's workspace or local replacement.
- Never edit generated code; change its authoritative inputs and pinned generator.
- Start with a small public package surface aligned with caller workflows; put
  implementation-only packages in `internal/` and keep optional adapters apart from
  core behavior. Do not mandate `pkg/`. Avoid package initialization that performs
  I/O, starts goroutines or mutates globals.

## Names, public APIs and values

- Format handwritten Go with `gofmt`; use its tabs and the repository's import grouping.
- Use MixedCaps and preserve `ID`, `HTTP`, `URL`, `JSON` and `RPC` initialisms.
  Avoid package stuttering and catch-all packages. Use American English.
- Keep public surfaces small and caller-oriented. Define interfaces at consuming
  boundaries or where public substitution is a supported contract, not for every type.
- Document exported declarations with comments beginning with their names.
  Explain nil/zero semantics, concurrency, ownership, cancellation, error identities
  and cleanup. Add executable examples for important public workflows.
- Design exported APIs from realistic caller examples first and export only what
  the library will support. Make zero values useful where possible; reject invalid
  construction early. Use plain parameters or config structs for simple
  configuration and functional options when extensibility outweighs the added
  namespace; document option defaults, validation, ordering and duplicates.
- Reuse standard interfaces such as `io.Reader` and `http.Handler`; prefer concrete
  returns unless an abstraction is part of the API. Use pointer receivers for
  mutation or values that must not be copied, and avoid public embedding that
  promotes dependency methods. Mark retired API with a `Deprecated:` paragraph
  naming the replacement.
- Use named domain values instead of interchangeable primitives. Defined types
  do not inherit codec methods; explicitly forward text/JSON codecs and test wire
  formats. See `cratis-fundamentals-concept-go` when using Fundamentals.Go.
- Specify `Close` ownership, repeated-close behavior and use after close, and
  document concurrent-use guarantees for clients, handles and callbacks.
- Use generics for meaningful type-safe reuse, not speculative abstraction; do not
  replace domain types with `any` for plumbing convenience.
- Document whether slices, maps, config and results are owned, copied or borrowed.
  Copying a slice/map header is not a detached copy. Do not copy a used mutex,
  wait group or atomic value, or expose pooled bytes after returning their buffer.

## Errors, context and diagnostics

- Return ordinary failures as the final `error`; inspect errors, including cleanup.
  Libraries must not exit the process or panic for expected failures.
- Preserve documented error identity with `errors.Is`/`errors.As`, not strings.
  Wrapping with `%w` exposes that identity as API; do it deliberately. Do not
  return an `error` interface containing a typed nil.
- Add useful operation context. Handle/log an error at one boundary rather than
  repeatedly logging and returning it. A partial or ambiguous write is not success
  and must not trigger a blind retry. Use sentinels for stable categories and typed
  errors for structured details; translate dependency errors whose identity must
  stay internal, and explain intentional suppression.
- Put `context.Context` first for blocking/cancelable work and propagate it through
  RPCs, HTTP, storage and callbacks. Never escape caller cancellation by replacing
  it with `Background`. Do not retain request contexts in long-lived clients.
- Call acquired cancel functions. Context values carry request metadata, not
  dependencies or configuration; use private keys with typed accessors.
- Prefer injectable `log/slog` at a supported minimum. Do not mutate global logging
  from a library. Redact credentials and sensitive payloads.

## Goroutines, streams and iterators

- Prefer synchronous APIs and let callers place concurrency. A saturated
  `errgroup.SetLimit` can block `Go`; use `errgroup` only when its cancellation
  and error behavior fits.
- Each goroutine needs an owner, termination condition and observable join path.
  Cancellation requests termination; it does not join workers.
- Bound workers, queues, subscriptions, buffers and retries; define backpressure.
  Make blocking operations cancellation-aware. Close channels only after senders stop.
- Do not hold locks while calling user code or performing slow I/O. Call
  `WaitGroup.Add` before launching work when not using version-supported `Go`.
- Stop immediately when iterator `yield` returns false. Release resources on
  exhaustion, early exit, failure and cancellation; call `iter.Pull`'s stop function.
  Specify ordering, errors and whether a stream can be consumed more than once.
- Preserve protocol metadata, tenancy, checkpoints and duplicate-delivery semantics.
  No generic retry or stream helper may promise exactly-once effects.
- Validate remote TLS identities. Development exceptions must be explicit.
  Bound untrusted payload sizes, fan-out and retained subscription state, and keep
  long-lived subscription lifetimes apart from unary-call deadlines.
  Use cryptographic randomness for secrets, never `math/rand`.

## Specifications and checks

- Use standard `testing`, meaningful subtests, useful got/want diagnostics and
  table-driven cases where setup/assertions actually match. Use external `_test`
  packages to exercise public contracts where practical.
- Use `t.Helper`, `t.Cleanup` and `t.TempDir`. Isolate state before `t.Parallel`.
  Synchronize with channels/joins, not guessed sleeps. Fake-time tests do not
  establish real-network behavior.
- Cover error identity, nil/zero input, cancellation/deadlines, partial outcomes,
  early stream exit and resource ownership. Bound fuzz duration and preserve
  failing seeds. Use race checks where supported; race-free does not mean leak-free.
- Add a regression test for each corrected behavior, including error and boundary
  cases. Use `httptest` or `bufconn` plus real-transport tests where needed, and
  protobuf-aware comparison for generated types.
- Profile or benchmark a demonstrated problem before optimizing; add no pools,
  caches or `unsafe` without measured justification.
- Run the repository's build, tests, formatting, vet and pinned golangci-lint gates,
  with race and vulnerability scanning as separate checks. Respect the declared
  minimum without silent toolchain upgrades. Report absent tools honestly.

## Compatibility and releases

- Prefer additive change: exported signature changes break callers, and adding a
  method to an exported interface breaks its external implementations. Check struct comparability, embedding, defaults, errors and wire shape;
  API-diff tools aid but cannot prove behavioral compatibility.
- Introduce `/v2` only with a migration and parallel-support plan. Never move a
  published tag; issue a corrective release and retract when needed.

## Adaptation and attribution

This condensed rule adapts the repository-owned Go baseline at
[Chronicle.Go v0.9.1](https://github.com/Cratis/Chronicle.Go/blob/v0.9.1/.cratis/ai/rules/go.md)
and [Arc.Go v0.1.1](https://github.com/Cratis/Arc.Go/blob/v0.1.1/.cratis/ai/rules/go.md),
not their project paths or commands. The baseline's adapted readability,
error-boundary, public-option and goroutine recommendations retain attribution:
[Google Go style](https://google.github.io/styleguide/go/guide.html), Google,
[CC-BY-3.0](https://creativecommons.org/licenses/by/3.0/); and
[Uber Go style](https://github.com/uber-go/guide/blob/master/style.md), Uber
Technologies, Inc., [Apache-2.0](https://github.com/uber-go/guide/blob/master/LICENSE).
No community code snippets are reproduced here.
