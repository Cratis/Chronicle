# Ada upstream backlog — Chronicle

**22 open items: 13 defect reports and 9 improvement proposals.** All were found while building **Ada**, a large production Cratis application — event-sourced backend on .NET 10, React/TypeScript frontend. Nothing here has been handed over upstream yet; you are the first reader.

**To work this list:** tell your agent *"work through `ADA-UPSTREAM.md`"*. Take the items in the order given, one at a time. Complete each — verify, reproduce, fix, gate, report — before starting the next. Do not batch items into one branch or one commit.

**Where the full reports live** (each is self-contained and cold-readable; read the whole file before touching code):

| | Path |
|---|---|
| Defect reports | `/Volumes/sourcecode/repos/hive/Ada/Planning/upstream/<ID>.md` |
| Defect index | `/Volumes/sourcecode/repos/hive/Ada/Planning/CRATIS_UPSTREAM_PROMPTS.md` |
| Improvement proposals | `/Volumes/sourcecode/repos/hive/Ada/Planning/upstream-improvements/<ID>.md` |
| Improvement index | `/Volumes/sourcecode/repos/hive/Ada/Planning/CRATIS_UPSTREAM_IMPROVEMENTS.md` |

Every citation in every report was taken at Chronicle **`623db1b95`** (`v16.13.3-4`). If HEAD has moved, `git diff 623db1b95 HEAD -- <cited file>` before trusting a `file:line`.

---

## How to work these

Ada is a consumer, not an authority on this codebase. Every report is a high-quality hypothesis with evidence attached — never a specification.

1. **Re-verify before anything else.** Open every cited `file:line` and confirm it still reads as reported.
2. **Trust the report's own honesty markers.** Each file has an *Honest limitations* section stating which claims were **observed live** and which were **traced by reading source**. A source-traced claim is unproven until you run it. Several reports also record claims of their own that were later withdrawn or corrected — read those, they mark where the reasoning was hardest.
3. **Reproduce inside this repo, at the right tier, before fixing.** Failing spec first, then the fix. Mutation-prove it: red before, green after, red again with the fix deleted. A spec that is green on both sides measures nothing.
   - ⚠️ **Tier is the main trap here.** Items 1, 3, 5, 6 and 7 are invisible to an in-memory harness and need a real kernel + real Mongo. Item 5 (CHR-34) reproduces **only on a replay** — a forward-only run failed to reproduce it and nearly got the report withdrawn.
4. **The "suggested fix" is a suggestion.** Chronicle owns the design. The *symptom* is the falsifiable part and must stop happening; the remedy is Ada's opinion, and you should reject it freely. If the right fix touches the contract or the proto, state the wire-compatibility verdict and blast radius first.
5. **Verify by behaviour changing, never by the code being present.** Item 1 (CHR-32) is this corpus's proof: the fix shipped, its specs pass, and it has never once run.
6. **If an item does not reproduce, that is a valid outcome.** Record what you ran and move on. If it is blocked on a ruling Chronicle owns, state the question and move on.
7. **Gates:** this repo's full build + spec gates, zero warnings, and cite the actual output — not a recollection of it.

**Improvement proposals (items 14–22) are read differently, and the difference matters.** A defect is a bug report — symptom, reproduction, fix. An improvement is a *design conversation*: nothing is broken, and the shape is Chronicle's call, not the implementer's. For those items:

8. **Start with the proposal's *"Proposal vs. established fact"* section.** It separates what was verified in this repo from what is Ada's *suggested shape*. Chronicle has agreed to none of the latter. An agent that skips this section confidently implements a design nobody chose.
9. **Respect *"⚠️ What is explicitly not being asked for."*** That is Ada drawing the line between a seam Cratis should own and Ada's own taste. Don't widen past it — and don't narrow the seam down to Ada's specific use either.
10. **The *Implementation brief* is the working document**: pinned commit, current behaviour at cited `file:line`, every touch point, existing specs and the specs the change needs, build/test commands, blast radius, wire-compatibility verdict, acceptance criterion, and what is still owed.
11. **Where an option is marked *needs design*, or a prerequisite is unmet — stop and put the question** rather than picking one. A proposal implemented past its open ruling is worse than one not started.

**Report per item:** which claims you **confirmed, refuted or corrected**, and the corrected mechanism — Ada wants these back, and a refuted claim is as valuable as a confirmed one. Then the reproduction, tier and mutation evidence (or, for an improvement, the option chosen and every open design question you had to answer); anything asked for that you deliberately did not do, and why; any adjacent defect you found on the way.

Work on a branch per item. Do not push or open a PR unless asked.

---

## The list

### 1. CHR-32 — the pre-16.12 constraint-store upgrade ships but never runs

**Defect · 🔴 Critical · re-opened 2026-08-02**

A 16.12+ kernel dies during constraint registration against a store written before 16.12. The fix commit `4b09223be` **is** an ancestor of `v16.13.2` and `v16.13.3`, its specs pass, and the store still takes the kernel down — the serializer carrying the legacy-document upgrade is never registered, so the upgrade never runs.

Blocks every production upgrade across the 16.12 boundary; an existing store leaves the client unable to start. Recorded as closed on source evidence alone on 2026-08-01, then re-opened when exercised live against an isolated harness. **This item is why rule 5 above exists.**

### 2. CHR-44 — one unregistered event type aborts discovery for EVERY projection

**Defect · 🔴 High**

An event type named in a model-bound projection attribute but never registered as an `[EventType]` throws out of `ModelBoundProjections.Discover()`, which builds every root projection in one unisolated `ToDictionary`. So **every** projection is lost, fluent ones included — and the `ApplicationStarted` callback the ASP.NET Core boot path uses swallows the exception.

Result: the host is healthy, commands append, no failed partition, no query-time error — the read side simply never updates, which is indistinguishable from *"nothing has happened yet"*. The **fluent path twelve lines away already isolates per projection and logs**, so the smallest acceptable fix is "do what the neighbouring branch does".

Ships with **item 20 (IMP-15)** — same boundary from the two sides Chronicle owns separately. Lead with this one. Two claims in the report's first description were wrong and are recorded as refuted inside it; the corrected chain is source-verified and the host-side swallow is measured.

### 3. CHR-42 — the testing harness materializes an empty child collection as `[]` while the live sink omits it

**Defect · 🔴 Critical · lead this pair, then item 4**

`ReadModelScenario<T>` seeds `[]` for every array-typed schema property and — unlike the live pipeline — writes it, because its four-line `HandleEventFor` omits the kernel's children-path exclusion (`HandleEvent.cs:81-96`). The live sink therefore omits an empty child collection and the .NET reader returns `null`. `CommandScenario<T>` inherits the harness answer by both its routes.

The severity is not the mismatch — it is the consequence: **no spec at any tier can catch item 4's defect class.** A consumer can have complete, green, honest coverage of exactly the path that throws in production. Ada shipped a user-reachable command that could never succeed, behind six green specs.

⚠️ The seeding itself is **not** harness-specific (`ProjectionFactory.cs:311-322` is the identical function on the live path); the divergence is the exclusion the kernel applies immediately afterwards and the harness never reaches. An earlier, stronger-sounding claim — that the harness *manufactures* a representation production never produces — was refuted and is recorded as withdrawn inside the report.

### 4. CHR-28 — an empty collection is absent, and the C# reader turns that into `null`

**Defect · 🔴 High · triage with item 3**

The sink's omission of an empty child collection is **deliberate** (concurrency). The defect is that the C# reader turns that absence into `null` behind a non-nullable `IReadOnlyList<T>`. The TypeScript half is fixed as of Fundamentals 7.16.8; this C# half is all that remains.

Presented alone this reads as a modest ergonomics ask about a null guard, and its 2026-07-10 filing sat for eight months at exactly that priority. Its measured cost arrived only in 2026-08-03: a guard mutation flipped an identical HTTP call from 400 to 200, and a sweep found 25 further unguarded sites across 11 files and 5 modules — several of them deterministic first-load failures, not races.

⚠️ **Fixing this and closing item 3 as "no longer reproducible" is the wrong outcome**, and both reports say so: the harness would then be accidentally right about collections, with nothing binding it to the reader for the next representation question. There have already been three (items 3, 5, 6).

### 5. CHR-34 — a projection replay rewrites every stored child whole, defaulting every unset member

**Defect · 🔴 Critical · pairs with item 6**

The write path is a **projection replay**, triggered by `[PII]` anywhere in the schema graph. It rewrites every stored child whole, writing each value-type member's CLR default and violating the kernel's own registered schema: the compliance step (`EncryptChangeset`) round-trips the state through a schema-driven converter that synthesizes a value for every absent property, and its only exemption tests the property's `format`, which an enum does not carry. Projection style and nullability are both irrelevant — proven fluent and model-bound, nullable and non-nullable.

One malformed child kills an entire observable query; the surface never loads. ⚠️ **The bytes are self-perpetuating** — they cannot be migrated away, because the next replay writes them again.

Two read paths, and the quieter one is arguably worse: one fails loudly, the other renders the stored document through the read model's schema, drops the out-of-schema value, and hands back the CLR default. A consumer cannot tell them apart. The report **names no reader for the throw** — that claim was downgraded to unconfirmed on 2026-08-03 and the report says so.

⚠️ **Do not hand this over in either pre-correction form.** Its original cause statement was withdrawn on 2026-08-02 and replaced. The report carries the confirmed write path with `file:line`; use the current text or none of it.

### 6. CHR-27 — the Mongo read-model sink drops a zero-valued projected enum

**Defect · 🔴 High · fix jointly with item 5**

An explicitly projected zero-valued enum is written as an absent field and reads back `null`.

⚠️ **Re-verify this one first — it has not been re-checked since 16.9.1.** If it no longer reproduces, that is itself the useful answer. It cannot be checked statically: the in-memory harness masks it, so it needs a booted kernel and a scratch read model. Ada's 1-basing workaround removed every zero-valued nullable-enum projection from its codebase, so there is nothing left in Ada to re-run it against.

**Why joint with item 5:** absence and `0` each carry two meanings today, so neither 1-basing nor 0-basing an enum is safe. The two workarounds are mutually exclusive; fixing item 5 alone leaves a consumer with no safe nullable enum at all.

### 7. CHR-23 — the release pass blanks a value that was never encrypted

**Defect · 🔴 High**

The release pass silently **blanks** a `[PII]` value when the resolved subject has **no encryption key** — indistinguishable from crypto-shred, with no error anywhere in the stack. 16.12.0 fixed the *neighbouring* case; the branch this reports is byte-identical through `v16.13.3`.

✅ The central claim is **measured** as of 2026-08-03 — a kernel-tier minimal pair, 18 `[KernelFact]`s, real kernel + real Mongo, mutation-proven both ways.
⚠️ The **suggested fix** is **not** measured — it is settled from source and was never run. Do not read the two as one.

A 2026-08-02 upstream assessment proposed reclassifying this as by-design; Ada's deciding live check refuted its stated mechanism. Both are recorded in the report.

### 8. CHR-35, CHR-36, CHR-37 — three `Cratis.Chronicle.CodeAnalysis` false positives

**Defect · one file: `CHR-35-36-37.md` · High / High / Medium**

- **CHR-35 — `CHR0002`** demands `[EventType]` on **every** generic argument of a fluent projection-builder call, so it hits the child model type of `.Children<T>`, the key type of `.IdentifiedBy<T>` and the join-key type of `.On<T>` — none of them an event. Ships as `Error`; 19/19 Ada sites are correct code.
- **CHR-36 — `CHR0004`/`CHR0005`** analyze a reactor's **private helper methods** as if they were handlers; the helper-skip guard keys on the return type, which accepts the `Task`/`Task<T>` a helper returns. `CHR0005` ships as `Error`.
- **CHR-37 — `CHR0010`** (and `CHR0008`/`CHR0009`/`CHR0011` via the same helper) synthesizes a `<default>` store for an event type with no `[EventStore]` and counts it as a second store, so a single-store projection reports as multi-store. Absence of the attribute means *unconstrained*, not a distinct store.

Together they produce **48 build errors on a clean consumer tree**. Static findings — an analyzer either reports a site or it does not.

### 9. CHR-29 — `Cratis.Chronicle.XUnit.Integration`'s nuspec omits the embedded kernel's runtime dependencies

**Defect · Medium**

The package embeds the kernel assemblies but not their package dependency closure, so the in-process silo dies on a `FileNotFoundException` cascade at first boot. The nuspec declares the same 9 dependencies at every version and never the embedded kernel's closure.

**The sharpened ask is the recurrence:** the omitted set **grew on a routine patch bump** — three more assemblies, an 8 → 11 workaround block in the consumer, and the whole spec tier failing at fixture init. The package is unusable out of the box.

Two smaller asks on the same package ride along in the report, deliberately unnumbered — test-infrastructure ergonomics with no correctness consequence. They ship in the same hand-over.

### 10. CHR-39 — `[NoAutoMap]` is silently ignored on `[ChildrenFrom]` child and `[Nested]` members

**Defect · 🔴 High**

Property-level `[NoAutoMap]` compiles, emits no diagnostic and **does nothing** on a `[ChildrenFrom]` child or a `[Nested]` type — the colliding event AutoMaps over the explicitly sourced value. `[Nested]` additionally ignores the class-level form and inherits the *root's* exclusion list by bare property name. The `CHR0025` analyzer *recommends* the inert placement (see item 16).

The child half **needs no kernel to settle**: `NoAutoMapProperties` is absent from `Contracts/Projections/ChildrenDefinition.cs` **and** from `message ChildrenDefinition` in `projections.proto`, so a per-property child exclusion cannot be transmitted to any kernel, forward or replay. Upstream's own spec suite corroborates the boundary: of four `with_no_auto_map` specs, the only property-level one is root-only and the only child one uses the class-level form.

Silent wrong values in a child collection, no build or runtime signal, and the sanctioned single-property fix *appears* to work while not working. **A compile-time diagnostic is an acceptable complete fix** in place of the capability.

### 11. CHR-41 — two `[SetFromContext<T>]` for the same event type on one property silently discard all but the last

**Defect · Medium**

`[SetFromContext<T>]` is `AllowMultiple = true` — correctly, for **distinct** `T`. Two attributes naming the **same** `T` on one member write the same `FromDefinition.Properties` key through an indexer, so the last declared wins and the earlier is discarded, with no build, registration or runtime signal. Three identical loops: root property, root parameter, `[ChildrenFrom]` child parameter.

Narrow shape, but it is **always** an authoring error — a scalar cannot hold two context values — so unlike the rest of this set it has **no second reading** and warrants a warning or error. **This is the hardest item in the silence-set to answer "no" to**; if the policy answer is *"diagnose only where there is no valid reading"*, this is the one that still ships.

⚠️ It carries a second lesson: this silence is what let Ada's own rule corpus encode, and carry for months, a **false constraint in the opposite direction**. See the report's *Archaeology*.

### 12. CHR-40 — a `message:` on a property-level `[Unique]` is silently discarded

**Defect · Low-Medium · the purest statement of the policy question**

A `message:` on a **property-level** `[Unique]` is never read; the class-level form reads the same argument through the same accessor. Nothing reports the discard. The constraint still enforces, so only the human-readable half is lost, invisibly.

⚠️ **The ask is a diagnostic, not the capability.** An attribute argument is a compile-time constant, so delivering the message hands a localizing consumer something it still cannot use; the capability half was demoted to an alternative on 2026-08-02. That makes this **the cheapest possible first yes** on the shared policy question behind items 10–12 — *when a consumer writes something the framework will not use, is silence acceptable?* — and the one item where delivering the capability would satisfy nobody the diagnostic would not.

**Never exercised.** Ada declares zero `[Unique(` sites, so there was nothing to run; source-verified only.

### 13. CHR-43 — three attribute docstrings promise projections filter on event metadata; nothing anywhere does

**Defect · Documentation · Medium-High**

The `<remarks>` on `EventStreamTypeAttribute`, `EventSourceTypeAttribute` and `FilterEventsByTagAttribute` all promise filtering *"when applied to an observer (reactor, reducer, **or projection**)"*. A projection filters on none of it, and the negative is settled at three independent layers:

1. **`ProjectionDefinition` has no filter member at all** — not an unpopulated one — in the contract, concept or storage representation, where `ReactorDefinition` and `ReducerDefinition` both declare `ObserverFilters Filters`. So a projection **cannot express a filter**, in any version, by any client.
2. `ProjectionsManager.cs:209-212` omits the `filters` argument that `Reactor.cs` and `Reducer.cs` pass — and the projection's *second* subscribe branch, `SubscribeToAllEvents<TSubscriber>`, has **no `filters` parameter at all**.
3. A null filter set short-circuits to `true` before any check is reached.

Chronicle's own `filtering.mdx:3` says the opposite, correctly. A filtered projection silently observes everything, with no build, startup or runtime signal — and the docstring is what IntelliSense shows at the moment of authoring.

⚠️ **`FilterEventsByTagAttribute.cs` sits at the client root, not under `Events/`** like its two neighbours — the short citations invite a wrong path during the fix.

**Pairs with Arc's `ARC-27`** (two owners, one hazard): on a command the docs claim a routing-only tag is concurrency-inert when it narrows the check; on a projection the docstrings claim the tag filters when it does nothing. Neither asks for a behaviour change — both ask the documentation to move to the code.

One unnumbered finding rides along: the client-side `EventStreamId.IsDefault` is inverted relative to its own doc comment, while the Kernel copy is correct and is the one the storage filters use. Measured zero consumers, so it is latent — but it is public API.

---

## Improvement proposals

From here down, nothing is broken. Read discipline points 8–11 above before starting any of these.

### 14. IMP-9 — the default unique-constraint violation message interpolates the offending value

**Improvement · ✅ Executable — the smallest change in the whole register**

**One line, wire-compatible, breaks zero specs.** The no-message default interpolates the **offending value** into a string Arc converts into `CommandResult.ValidationResults` — i.e. a response body. Over a uniqueness constraint, that value is an email, an organization number or a person identifier. Internal ids ride along in both defaults.

Ada's cost is the coverage built so the default is unreachable: 26 of 32 constraint classes carry a localized callback, 6 are guarded exemptions, held by two ratcheted source-scanning meta-specs.

### 15. IMP-11 — a spec cannot ask Cratis which artifacts it registered

**Improvement · ✅ Executable — ~90% already shipped**

**One property.** Cratis knows which types it registered; a spec cannot ask, so every consumer re-derives the registry by reflection. `Defaults.Instance.ClientArtifactsProvider` is already public and Arc already uses it. Chronicle + Arc.

### 16. IMP-12 — `CHR0025` recommends `[NoAutoMap]` where the attribute is inert

**Improvement · ⚠️ Item 2 executable · item 1 needs design**

`CHR0025` recommends `[NoAutoMap]` on child and nested records where the attribute does nothing, and the attribute's own docs promise the same. **Item 2 is one sentence of XML doc.** Item 1 — a compilation-wide reverse lookup — needs a ruling on whether it is worth it.

Pairs with **item 10 (CHR-39)**: the defect is that the attribute is inert there; this is that the tooling actively steers you into it.

### 17. IMP-7 — widen `CHR0039`: a discarded `Task`-returning assertion is a silent no-op

**Improvement · ✅ Executable**

**Two gate edits in one 116-line file, plus specs.** `CHR0039` misses `Task<T>`/`ValueTask`, any `Should*` outside `Cratis.*.Testing.*` — **including Chronicle's own `XUnit.Integration.Events` assertions** — and non-extension call shapes.

⭐ Ada had **545 silent no-op assertions** that existed and had to be closed before the rule was useful; probe-verified with planted defects. ⚠️ One of its three axes was already covered — check before implementing.

### 18. IMP-4 — the in-memory spec harness substitutes five layers and signals none of it

**Improvement · ✅ Option 1 (docs) · option 2 executable · option 3 needs design**

`ReadModelScenario<T>` substitutes the sink, storage/lifecycle, the event context, `[Join]` key resolution and deferred handling — and emits no signal that it has done so.

⭐ **Three filed defects lived in exactly those layers** (CHR-18, and items 6 and 5 above); one was a live outage with **5,869 specs green**. ⚠️ Packaging trap on any new public API. **Do not start option 3 before item 9 (CHR-29).**

### 19. IMP-15 — registration has no observable outcome

**Improvement · ⚠️ Shape is the open question**

A consumer that needs to know whether its read side came up has to re-drive `Discover()`/`Register()` and catch. Three candidate shapes, and choosing between them is Chronicle's call: the awaitable-completion option is client-only and executable; a per-artifact result is a contract change and **wire-breaking unless added as a new operation**.

**Ships with item 2 (CHR-44)** — the defect is *the failure is lost*; this is *the outcome is not askable*. Either can ship alone, and fixing only the defect still leaves a consumer unable to assert its read side came up. Ada's cost: a four-line `EnsureProjectionsRegistered` workaround that exists purely so a harness can tell a registered read side from an unregistered one — after a harness reported "no drift" over a read side it had never projected.

### 20. IMP-16 — a consumer that needs "absent means empty" has to reach past Chronicle to the MongoDB driver

**Improvement · ✅ Both docs options executable · 🔶 the policy option needs design**

The obvious in-language fix is silently dead. The docs options are 1–2 pages; the policy option is a read-model-boundary option across the reader, the command-side release path and an options surface. Chronicle + Arc.

✅ **Precondition discharged 2026-08-03** — nothing blocks this.

### 21. IMP-13 — the shipped optimistic strategy produces the exact scope state the kernel calls a caller bug

**Improvement · ✅ Options 1 + 2a executable · 2b and 3 need design**

So the first append into any narrowed scope is never checked. Options 1 + 2a are 2 doc pages plus 1 log message. ⚠️ **Option 3 is a behaviour change** — a contract state, a kernel branch and a client mirror.

**Do not start before a ruling on whether an empty scope should be *checked* or merely *reported empty*.** Chronicle + Arc.

### 22. IMP-6 — `IReadModels` has no non-key query surface

**Improvement · 🔶 Needs design, not implementation**

No filtered `IQueryable`/`ISubject` surface and no non-key find, so every list, paged and observable surface drops to `IMongoCollection<T>` — which `ReadModelScenario<T>` cannot then reach. It governs **every** Ada query surface and compounds into an untestable shape.

Touches contract + proto + kernel service + client + harness. ⚠️ **Its own option 3 was refuted.** Do not start before a ruling on whether read models are queryable at all. **Bring the question, not a patch.**

---

## Cross-repo notes

| This repo's item | Pairs with | Where |
|---|---|---|
| 13 (CHR-43) | `ARC-27` — the same attributes documented wrongly on the command side | Arc |
| 15 (IMP-11), 20 (IMP-16), 21 (IMP-13) | Arc-side halves of the same change | Arc |

`CHR-38` carries a `CHR-` id but is **AuthProxy** work — it was filed into the Chronicle number sequence, not the Chronicle repo. It is not in this list.
