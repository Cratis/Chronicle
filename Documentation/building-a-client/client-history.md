# How TypeScript, Elixir, and Kotlin were built

The layering described in [Layering an idiomatic client](./layering-an-idiomatic-client.md) isn't
a plan that was designed up front and then followed three times. It's what actually happened, in
three separate repositories, in three different orders, and it's worth knowing the real story —
including the parts that didn't go smoothly — before starting a fourth.

## The one thing all three got from Chronicle itself

Before any of the three repos below existed, Chronicle's own release pipeline already generated
and published a contracts package per language, gated on a `wire-compatibility` check, in lockstep
with the kernel's own version number. That's covered in
[Two ways to start](./starting-points.md) and isn't repeated here — what follows is what each team
built *on top of* that foundation, and it's the part that varied.

## Chronicle.TypeScript

TypeScript's client was scaffolded almost entirely by an autonomous coding-agent session against a
near-empty repository — one commit added the connection layer, the event log, and a Node.js test
app all at once. That's a different bootstrap style from the other two, and it shows in the pace of
what followed: a decorator-based artifact-discovery system (`@eventType` and friends) landed within
a day, the repo was migrated to Yarn v4 workspaces shortly after to make the client and its console
sample buildable as one unit, and a full build-and-publish pipeline for `@cratis/chronicle` on npm
followed within the same week.

One early fix is worth calling out on its own: switching the package's module output to ESM to
resolve a conflict with `@cratis/chronicle.contracts` — direct evidence that the idiomatic package
was already depending on the separately published contracts package, and that module-format
mismatches between a hand-written idiomatic package and a generated dependency are a real thing to
watch for, not a hypothetical one.

Today `@cratis/chronicle` is the only publishable module in the repo — there's no TypeScript
equivalent of a convenience/hosting package yet (no Express, Fastify, or NestJS integration). It
ships as a deliberately fine-grained set of subpath exports (`./events`, `./projections`, `./jobs`,
`./webhooks`, and about twenty others) rather than one monolithic entry point, and publishes via
npm's OIDC trusted-publishing flow rather than a stored token.

## Chronicle.Elixir

Elixir went the opposite direction: a single large, hand-authored commit landed the entire initial
client — connection, event log, constraints, projections, reactors, reducers, read models — plus a
full console sample, in one shot. Its `mix.exs` already depended on
`{:cratis_chronicle_contracts, ">= 0.1.0"}` at that very first commit, confirming the contracts
package existed and was consumable before the idiomatic client did.

The most instructive moment in Elixir's history is a compile failure that only showed up once the
contracts package became a *real* external dependency rather than something built alongside it in
the same checkout: code that built struct literals against the contracts package's types at compile
time worked fine while both were in the same workspace, and broke the moment `cratis_chronicle_contracts`
was fetched as an ordinary Hex dependency. The fix was to build those structs at runtime instead.
It's a concrete warning for any client: test against the *published* contracts package early, not
just a local build of it — some failures only exist across that boundary.

Elixir also had to resolve an OTP application-name collision (renaming the app from `:chronicle` to
`:cratis_chronicle` while keeping the Hex package name `cratis_chronicle`), and added a CI job that
runs *after* publishing specifically to verify the just-published package is actually installable
and usable — not just that it built.

Like TypeScript, Elixir has one publishable module (`cratis_chronicle`) and no convenience/hosting
package yet — no Phoenix integration exists. Its documentation and README have also drifted further
behind its actual feature set than the other two repos', which is worth noting as a caution rather
than following as a model: keeping docs in step with a fast-moving client is a real cost, not a
byproduct.

## Chronicle.Kotlin

Kotlin is the newest of the three and, so far, the most structurally complete. Like Elixir, it was
hand-built first by the maintainer in a dense initial commit, with agent-assisted work picking up
afterward. Two things distinguish its trajectory:

**Java support arrived deliberately, within a week of the Kotlin client existing at all.** Kotlin's
`suspend` functions aren't callable from Java, so the client grew a hand-written blocking bridge
class per service (`EventLogJavaBridge`, `ReadModelsJavaBridge`, and so on) rather than asking Java
consumers to deal with coroutines. Supporting two languages from one artifact turned out to need an
explicit safeguard, too: a binary-compatibility validator was added specifically because adding a
property to a Kotlin data class silently breaks any Java caller using a positional constructor —
without the validator, that kind of break had already happened twice before it was caught in
review rather than in someone's build.

**A Spring Boot starter followed, once — and only once — the idiomatic client had artifact
auto-discovery to build on.** `io.cratis:chronicle-spring-boot-starter` landed about two months
after the Kotlin client's bootstrap, in a single ~1000-line commit covering auto-configuration,
`ChronicleProperties`, and per-request namespace resolution (fixed, HTTP-header, subdomain, or
authentication-claim based). It's the direct JVM counterpart of `Cratis.Chronicle.AspNetCore`, and
its existence is the clearest confirmation of the convenience-package layer being a real, proven
pattern — it's just the one client so far that's actually built it.

Kotlin also added a fourth module, `io.cratis:chronicle-testing`, for in-process test support — the
optional testing layer mentioned in
[Layering an idiomatic client](./layering-an-idiomatic-client.md#convenience-packages-sit-above-that-and-are-optional).

### A real gap worth learning from

As of today, Chronicle.Kotlin's publish workflow releases only the `Source` module to Maven
Central. The `Testing` and `Integrations:SpringBoot` modules have had valid Maven coordinates and
working build configuration since the commit that added them — but the CI publish job list was
never updated to include them, so neither has actually shipped a release through automation. It's
a small, easy mistake to repeat: adding a new publishable module's build configuration is necessary
but not sufficient. The CI publish pipeline needs the same update, explicitly, or the module simply
never reaches its registry no matter how correct it is.

## What this means for a fourth client

- Don't wait to have the "right" bootstrap style figured out — Chronicle's three clients started
  three different ways (agent-scaffolded, hand-built dense-commit, hand-built dense-commit) and all
  converged on the same shape.
- Test against the *published* contracts package, not just a local build of it, before you trust
  that dependency boundary.
- A convenience/hosting package is worth building once the idiomatic client is stable and there's
  an obvious dominant framework to target in your ecosystem — not before, and not automatically.
- If you add a new publishable module later (a testing package, a hosting package), double-check
  your CI publish job actually includes it. It's the single most common way this goes wrong.
- Keep documentation moving at the same pace as the client, deliberately — it's the thing most
  likely to fall behind if it isn't treated as part of the same unit of work. See
  [Documentation and snippets](./documentation-and-snippets.md) next.
