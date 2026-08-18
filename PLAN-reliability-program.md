# Chronicle Reliability Program — Execution Plan

> **For the AI session picking this up:** this file is self-contained. Read it top to bottom
> once, then pick the first task on the Status Board that is `todo` and whose dependencies are
> done. Work one task per branch/PR. Update the Status Board in this file as part of each PR.
> Do not re-run the year-long analysis that produced this plan — the findings are summarized
> below and in `REPORT-regression-program.md`; the full evidence (charts, 73-incident taxonomy,
> severe-incident list) is the published artifact "The Regression Ledger"
> (https://claude.ai/code/artifact/4684e4d9-5e44-4a54-9ed8-4ea1afb9d3c8).

---

## 1. Why this program exists (compressed diagnosis)

Window analyzed: 2025-08-17 → 2026-08-17, vs the prior year.

- Commits 7,288 vs 3,069 (2.4×). Reverts 79 vs 11 (7×). 394 releases, **303 of them patches (77%)**.
- Of 14 sampled patch releases, **7 fixed a regression introduced in the previous few releases**;
  median time-to-hotfix 1–2 days. Users are effectively the canary.
- Issue inflow +41% (304 vs 215); 90 of 304 use regression language; **60 say "silently"** — the
  dominant failure mode is wrong/missing data with no error.
- ≥28% of commits are directly AI-attributed (floor; agent-session commits under human names not
  counted).
- 73 incidents classified by root cause: **G fix-induced 20** (timing-coupled observer/projection
  core), **E concurrency/stale-state 15**, **D cross-cutting-change-missed-a-consumer 9**,
  **B CI blind spot 8**, **H other incl. history-integration drift 8**, **A vacuous
  verification 5**, **C in-memory/engine contract drift 5**, **F autofix bots 3**.
- Proof the lever is verification, not authorship: after the June–July 2026 guardrails
  (`.ai` corpus, `framework.md` fidelity rules, 45 CHR analyzers, harness convergence), the
  fix/revert commit share **halved (26% → ~10%) while volume stayed near peak**.
- GitHub Releases evaluation: of the last 400 releases, **zero are prereleases and zero are
  drafts** — every merge became an immediately-consumable stable release, and the prerelease
  channel is entirely unused. **Known-broken releases carry no warning**: v15.37.0–v15.38.2
  (unbootable server images) and v16.15.0–v16.19.2 (Testing package broken for every consumer,
  #3598) are indistinguishable from good releases on the releases page; nothing is retroactively
  marked, deprecated, or yanked. Recent 16.x notes are high quality (user-facing, issue-linked),
  but internal template sections leak into public notes (v16.13.4 ships an empty "## Test plan"
  heading) and some notes are bare one-liners (v15.37.0).

**Program thesis:** at AI velocity, code production is no longer the constraint — trustworthy
verification is. Every item below is a machine-checked gate with a testable definition of done,
because prose rules erode (a pipeline sat red for 319 consecutive runs over 3 months; a CI
path-filter "speed fix" silently created the hole an unbuilt dependency bump later merged through).

---

## 2. Working agreements (read before touching anything)

These come from the repo's `.ai/` rules, the user's global instructions, and hard-won session
experience. They are not optional.

**Repo profile & gates**
- This is the **framework profile** (`.ai/rules/framework.md`): libraries, not vertical slices.
- Quality gates for every PR: `dotnet build` clean **Debug and Release, zero warnings** (CI treats
  warnings like CA1848 as errors — build Release locally before pushing), `dotnet test` green for
  affected projects, `yarn lint` / `npx tsc -b` for TS changes.
- Specs use `Cratis.Specifications` (`Establish`/`Because`/`should_` + NSubstitute), the plain
  `Specification` base — **not** IClassFixture, even for container-backed specs.

**Git & PR discipline**
- **Never force-push or rewrite published history. Never amend/rebase pushed commits.** Additive
  history only; `git revert` to undo.
- **Wait for the user's approval before committing.** Prepare changes, show them, then commit on
  approval. Use the `ship-changes` skill for branch/commit/PR mechanics when told to ship.
- **Never merge PRs yourself** — push and let the user merge. No exceptions; the earlier docs-only
  carve-out is withdrawn.
- **Most of this program's work must carry NO semver label.** The label is the decision to ship, and
  almost everything here — CI workflows, scripts, gates, specs — changes nothing a consumer compiles
  against or runs. Label only a PR that genuinely alters shipped behavior. Labelling infra work
  `patch` is what turned eight of the eighteen releases on 2026-08-18 into releases of nothing. The
  `verify` job failing for a missing label is the **expected** state for these PRs; every other check
  still has to be green. See `.ai/rules/pull-requests.md`.
- **Group related tasks into one PR.** The PR is the release boundary. Several small merged PRs
  become several releases; collect a body of work onto one branch as separate commits.
- PR descriptions become release notes verbatim: Added/Changed/Fixed/... sections only, written for
  framework users; pure internal plumbing (most of Phase 0) gets a minimal, honest description.
- After pushing, monitor CI via the GitHub MCP tools (`pull_request_read` → `get_check_runs`,
  `get_job_logs`) and fix failures. Trust `mergeStateStatus=CLEAN`, not `gh pr checks` — the
  coverage `[skip ci]` commit shifts HEAD and makes checks look "3 pass / 7 skip".

**Operational gotchas (will otherwise cost you hours)**
- A shell hook routes commands through `rtk`, which **truncates output at ~50 lines**. For any
  command whose full output matters, use `rtk proxy <cmd>` redirected to a file, then grep it.
- `gh pr edit` (body/labels) **silently no-ops** on this repo (Projects-classic GraphQL). Use the
  REST API and verify the change landed.
- `Chronicle.slnx` sometimes shows unexplained dirt; a dirty tree **silently aborts `git merge`** —
  after any merge, verify it actually landed.
- In-process integration specs flake in CI. Reproduce/verify **locally** first (MongoDB on
  `localhost:27017`).
- Never run just a new spec in the `for_ReadModelReactors` shared collection — a 2nd
  `IReadModelReactor` spec can hang the whole suite (~1h CI stall). Run the **full collection**
  locally with `--blame-hang-timeout` for any change there.
- Out-of-process integration locally: publish with `-r linux-<arch>`, run the kernel container,
  read kernel trace via `docker logs`. OOP observers run in the **test-host silo**, not the
  container. For join/projection changes also run `Testing.Specs`.
- Client integration tests need the dev symbol + env vars or they silently run a tiny subset
  (see memory/`running-client-integration-tests-locally`).

---

## 3. Phase 0 — this week: no broken release can ship

Six independent tasks, each a small PR. No dependencies between them; do in any order.

### Task 0.1 — Boot gate on every published Docker image
**Traces to:** images 15.37.0–15.38.2 unbootable (exec bit stripped by `actions/upload-artifact`
on the publish→docker hand-off, commit `716150a73`); every *development* image since 16.33.1
aborting at startup (a rebase reintroduced a runtime env-var check DEVELOPMENT-symbol images
can't satisfy, fixed `f52bc884f`); `latest-development` entrypoint launching a missing binary
(#3250). **No CI job has ever started a published image.**

**Implementation**
1. Enumerate every docker publish job in `.github/workflows/publish.yml` (at last check:
   `publish-docker-production`, `publish-docker-workbench`, plus any development-image job —
   verify the current set) and any development-image workflow.
2. For each: before the multi-arch `docker/build-push-action` push, build the **native-arch**
   image with `load: true`, `docker run -d` it **with default/consumer-like environment**
   (no special env vars — that's exactly how the 16.33.1 dev images died), and poll the health
   endpoint (the kernel exposes health ports; check `Docker/*/Dockerfile` and server config for
   the port) with a timeout. Non-healthy ⇒ fail the job ⇒ nothing is pushed.
3. Boot-test the development variant with its DEVELOPMENT-symbol behavior — its code paths differ
   from production; testing only production would have missed the 16.33.1 break.
4. Don't boot-test the QEMU cross-arch build (too slow/fragile); native arch catches the known
   classes (missing exec bit, missing binary, startup abort).

**Done when:** a branch that deliberately strips the exec bit from the packaged binary fails the
publish workflow at the boot gate (verify once via `workflow_dispatch` on a fork/branch, or
replicate the gate step locally with `act`-style dry run + a local `docker run` proof).

### Task 0.2 — Consumer smoke for published packages
**Traces to:** #3598 — the published `Cratis.Chronicle.Testing` package baked in
Screenplay.Secrets types, so **every consumer's specs failed** against Screenplay 2.0.0; all of
16.15.0–16.19.2 affected. Packing succeeded, publish succeeded, every consumer broke.

**Implementation**
1. In `publish.yml`, after `dotnet pack` / npm pack and before any push to NuGet/npm: set up a
   throwaway consumer project (kept under `Integration/ConsumerSmoke/` or similar so it's
   version-controlled) with a `nuget.config` pointing at the just-packed artifacts directory.
2. The consumer references the published package IDs (Chronicle client, `Cratis.Chronicle.Testing`)
   at the *latest ecosystem versions of everything else* (floating transitive deps — the point is
   to catch skew like Screenplay 2.0), builds, and runs one real `EventScenario`-based spec.
3. Same idea for the TypeScript client: `npm pack` → install into a minimal consumer → `tsc`.
4. Failure blocks publish.

**Done when:** replaying the #3598 skew (pin the consumer's Screenplay to the version that broke)
fails the smoke and blocks publish.

### Task 0.3 — Silent-success sweep
**Traces to:** proto generation failed **22 of 23 packages, printed the errors, and exited 0** for
months — Kotlin/TS/Elixir clients were published from stale schemas (`889a9c546`/`ea4f7680a`,
whose own fix was reverted within hours — this area is still open, see churn note in §6); the
proto reserved-fields generator lived in **no solution**, so no gate ever compiled it
(`9f0d73062`); an integration CI job once ran **zero specs**, green (`7d33f25a6`).

**Implementation**
1. `generate-protos.sh` (and every script under `Tools/`/workflows): `set -euo pipefail`, remove
   catch-print-continue patterns, propagate per-package failures to the exit code. Grep all
   workflow `run:` steps for `|| true`, bare `catch`, and swallowed loops.
2. Add every tool/generator that CI executes to `Chronicle.slnx` (watch the slnx-dirt gotcha in
   §2) so the normal build gate compiles it.
3. In every test job (`dotnet-build.yml`, `integration.yml`, TS test workflows): assert the
   executed-test count is > 0 — parse the `.trx`/reporter output; a run with 0 executed tests
   fails the job with a message saying what that means.

**Done when:** (a) a deliberate generator failure turns the workflow red; (b) a test invocation
whose filter matches nothing turns the job red.

### Task 0.4 — Path-filter audit
**Traces to:** `47a82ad45` (2026-04-17) narrowed triggers to `.cs`/`.csproj` for speed; on
2026-08-05 a Fundamentals bump via `Directory.Packages.props` — "the riskiest change in the
repository" — **merged with no build at all** (`c9fbc8d99`). Benchmarks broke while landing green
(`51590103c`, `Benchmarks/**` unfiltered). The Docker exec-bit *fix* couldn't cut a release
because publish only triggered on `Source/**` (`fd48b6471` — since fixed with `Docker/**`;
verify it's still there).

**Implementation**
1. Inventory `paths:`/`paths-ignore:` filters across all workflows in `.github/workflows/`.
2. Ensure build+test triggers include at minimum: `Directory.Packages.props`, `Directory.Build.*`,
   `global.json`, `Benchmarks/**`, `Docker/**`, `.github/workflows/**`, `*.slnx`, `nuget.config`.
3. Add a comment block at the top of each `paths:` list naming this task and the rule: *changing a
   filter requires stating in the PR what stops being covered*.

**Done when:** a draft PR touching only `Directory.Packages.props` triggers the full build+test
workflow.

### Task 0.5 — Red-workflow alarm
**Traces to:** the PR-prerelease pipeline was dead for 3+ months — **319 consecutive failed runs,
every job skipped, zero artifacts** — and nobody reacted (`a73449bbd`). Alarm fatigue, not a
blind spot.

**Implementation**
1. New scheduled workflow (daily): via `gh api`, for each workflow in the repo take the last 3
   completed runs on `main`; if all failed and no open issue titled
   `CI: <workflow> failing repeatedly` exists, file one listing the run URLs.
2. Remember `gh` output goes through rtk — use `rtk proxy gh api ...` and write JSON to a file.

**Done when:** pointing the check at a workflow with ≥3 consecutive red runs auto-files the issue
(test by temporarily lowering the threshold or against the historical prerelease workflow data).

### Task 0.6 — Autofix bots become suggesters
**Traces to:** Copilot Autofix committed 96 mechanical rewrites this year; two broke the build
outright (`c237e5e74` LINQ autofix vs the build's own analyzers; `aedafe1bc` IDE0037 vs
warnings-as-errors Release), and the bot repeatedly rewrote hot kernel files.

**Implementation**
1. In-repo part: add `.github/CODEOWNERS` covering the hot-core paths (list in Task 1.3) so any
   change there — bot or human — requires maintainer review.
2. Human part (**needs the user, cannot be done from the repo**): in org/repo settings, remove
   direct-commit for autofix bots so their output arrives as PR suggestions; those PRs then face
   the normal Release warnings-as-errors gate.

**Done when:** CODEOWNERS is active on hot-core paths, and the user confirms bot direct-commit is
off.

### Task 0.7 — Release hygiene on GitHub Releases
**Traces to:** the releases-page evaluation in §1 — broken releases are unmarked and
indistinguishable from good ones, so consumers (and dependency bots) keep pulling them; the
prerelease channel sits unused; internal template sections leak into the public notes that
`pull-requests.md` says must be release-note-ready.

**Implementation**
1. **Retroactive warnings** (do first, it protects users today): edit the release bodies of the
   confirmed-broken ranges to open with a warning line and a pointer to the fixed version —
   `v15.37.0`, `v15.38.0`, `v15.38.1`, `v15.38.2` ("server Docker images in this release cannot
   start; use v15.38.3 or later") and `v16.15.0`–`v16.19.2` ("Cratis.Chronicle.Testing in this
   range is incompatible with Screenplay 2.0 (#3598); use v16.19.3 or later"). Before editing,
   re-verify each range boundary against the fix commits/issues. Use the REST API
   (`gh api -X PATCH /repos/cratis/chronicle/releases/<id>`) — remember `gh release edit`-style
   shortcuts and `gh pr edit` have silently no-opped on this repo before; **verify the body
   actually changed** after each PATCH.
2. **Ongoing rule** (encode in `.ai/rules/pull-requests.md`, same PR as Task 2.3's rule): when a
   hotfix fixes a regression that shipped in release N, editing release N's notes with a
   superseded-by warning is part of completing the hotfix.
3. **Notes-quality gate**: in the publish workflow, before creating the GitHub release, reject or
   strip bodies containing internal template leftovers — `## Test plan`, empty section headings,
   "Original prompt"/agent-transcript blocks. This makes the existing prose rule machine-checked.
4. **NuGet/Docker side** (**blocked-on-user**): deprecating the broken NuGet package versions on
   nuget.org and (if desired) removing the broken Docker tags requires owner action in those UIs;
   list the exact versions for the user from step 1.

**Done when:** every confirmed-broken release opens with a warning + pointer; a test release body
containing `## Test plan` is rejected by the gate.

---

## 4. Phase 1 — weeks 2–4: green must mean correct

### Task 1.1 — Shared behavioral contract suite for storage
**Traces to:** in-memory event-sequence storage treated the "do not narrow" sentinels
(`EventSourceId.Unspecified`, `EventSourceType.Unspecified`, `EventStreamType.All`,
`EventStreamId.Default`, empty event-type set) **as values to match** — every read returned
nothing while appends succeeded, and specs passed vacuously (`fea54b2c0`); in-memory returned
migrated content stamped with the old generation where persistent providers report the highest
(`2d1def358`); SQL sink silently dropped `PropertiesChanged` for collection children
(`b6a014216`); SQL-sink PII projections stalled on schema round-trip drops MongoDB tolerated
(`4774adf56`, #3463). **Structural cause: `Storage.InMemory.Specs`, `Storage.MongoDB.Specs`, and
`Storage.Sql.Specs` share no code — verified: none of them references `Storage.Specs`.**

**Implementation**
1. First inspect `Source/Kernel/Storage.Specs` — decide whether it becomes the shared abstract
   suite or a new `Storage.Contracts.Specs` project is cleaner. Pattern: abstract spec classes
   with an abstract factory ("give me an `IEventSequenceStorage`"), one thin driver project (or
   driver classes) per implementation.
2. Start with the interfaces that already burned: `IEventSequenceStorage` (append/read/sentinel
   semantics — encode the §framework.md sentinel rule as executable specs; migration/generation
   stamping), then the sink contract (child collections, `PropertiesChanged` on nested paths,
   document key + `__subject` round-trip), then `IEncryptionKeyStorage` (idempotent provisioning —
   see `e165c1ccf`).
3. Drivers: InMemory runs everywhere; MongoDB against localhost:27017 locally / the CI container;
   SQL via SQLite (note `SqlitePragmaConnectionInterceptor` exists for a reason — WAL +
   busy_timeout; and SQLite completing inline once *hid* a scheduler deadlock (`1f746db9e`), so
   where feasible run PostgreSQL in CI too).
4. Grow incrementally — the suite doesn't need to cover everything to start paying; it needs to
   cover the incidents above first.

**Done when:** the sentinel-filter bug, deliberately reintroduced in the in-memory implementation
on a branch, fails the shared suite's in-memory run.

### Task 1.2 — Finish harness convergence onto the real engine
**Traces to:** the in-process harness diverged from the kernel for months; specs green,
production wrong. Known fidelity list (each was a real incident): PII apply/release handling,
`ChildRemoved`, children-path exclusion, child-key resolution (`7ffb04fd9`), string-concept-keyed
`[Join]` (crashed **only** the harness — the real engine worked; fix belongs in the harness,
never the shared engine, commit `6858c9f52`), reducer initial state, EventScenario's in-process
`IConstraints` grain stub going stale after kernel changes (`e0595454e`). The July convergence
work (`a8223c009`, `29edba612`) pointed the harness at the kernel's real in-memory storage and
immediately exposed the Task-1.1 divergences — finish that direction.

**Implementation**
1. Enumerate what `Source/Clients/Testing` still stubs/reimplements instead of using kernel
   components; converge each onto the real code path, or — where impossible — add a spec pinning
   the harness to the engine behavior.
2. Close the fidelity list above; each item gets a spec that **fails against the pre-convergence
   harness** (that's the evidence the gap is really closed).
3. The out-of-process integration suite is the oracle when harness and engine disagree — never
   "fix" shared engine code to make the harness pass (that exact move broke real MongoDB joins,
   `95bcbcf7e`).

**Done when:** every fidelity-list item has such a spec, and `ReadModelScenario`/`EventScenario`
run through real kernel storage rather than parallel implementations.

### Task 1.3 — Contain the hot core
**Traces to:** 20 of 73 incidents are fixes that broke a sibling path, concentrated in five
areas; two rollbacks deliberately reinstated known bugs (`b3e03dbb8`, `d1c168c7e`). The OOP
integration matrix is the only oracle that has reliably caught this class.

**Hot-core paths** (verify exact current paths before wiring):
- `Source/Kernel/Grains/Observation/**` (esp. `Observer.Handling`)
- Projection key resolution (`KeyResolvers`) and `Changeset`/changeset consolidation
- `AppendedEventsQueue`
- `Source/Kernel/Core/Compliance/**` (PII/encryption)
- The SQL and MongoDB sink implementations

**Implementation**
1. In the PR workflow, add a changed-paths condition: any PR touching these paths must run the
   **full out-of-process integration matrix** and have it green to merge (make it a required
   check for those paths — GitHub required-checks are repo-wide, so implement as a job that
   no-ops green when paths don't match and runs the matrix when they do).
2. Add the same paths to CODEOWNERS (shared with Task 0.6).

**Done when:** a draft PR touching `KeyResolvers.cs` shows the OOP matrix as a required, running
check; a PR not touching hot paths doesn't pay the cost.

### Task 1.4 — History-integration guards for parallel agent work
**Traces to:** a stale agent worktree silently reverted a sibling branch's merged compliance
hardening — **CI stayed green because the specs were deleted along with the code**
(`9404b2b42` → restored `b1b0e1553`); a main-merge dropped the SQL OpenIddict storage layer
(`520bb50fe`); a rebase reintroduced dead code that bricked all dev images (`f52bc884f`).

**Implementation**
1. **Spec-deletion tripwire** (CI job on PRs): if the diff deletes files matching
   `*.Specs/**` / `when_*/**` / `for_*/**` without deleting the corresponding code under spec,
   fail with an explanation; an explicit `specs-removal-intended` label overrides. This single
   check would have caught the flagship incident.
2. **Branch freshness**: require branches up to date with `main` before merge (GitHub merge
   queue / "require branches to be up to date" — settings change, needs the user). Note the
   repo's own related trap: use `git cherry` to judge unique work on divergent branches, never
   ahead/behind counts.
3. **Agent-side**: add a rule to `.ai/rules/git-commits.md`: before pushing, verify the worktree
   contains `origin/main` (`git merge-base --is-ancestor origin/main HEAD` after a fetch); a
   worktree behind main must merge main first (merge, not rebase — additive history).

**Done when:** a PR replaying the `9404b2b42` shape (delete hardening + its specs) is blocked by
the tripwire; the user confirms the branch-protection setting.

---

## 5. Phase 2 — this quarter: dismantle the regression factory

### Task 2.1 — Deterministic completion signals in the observer/projection core
**Traces to:** category G (20/73). `63cf43e38` admits integration tests depend on "the implicit
50ms settling buffer" — the tests encode timing coupling, so any latency-shifting change in the
core regresses siblings, which is why fixes here keep getting rolled back.

**Implementation (design-first — do not jump straight to code)**
1. Write a short design doc (in `Documentation/` or as a PR discussion): an explicit
   "caught up / consolidated" acknowledgement surface on observers/projections — an awaitable
   "observer X has processed up to sequence N" the kernel already conceptually tracks — exposed
   to the testing infra (and operators) so tests await facts instead of sleeping.
2. Migrate the integration suites off fixed delays incrementally, hot-core suites first
   (`for_Reactors`, `for_Observers`, projection suites).
3. This unlocks safely reattempting the fixes that were rolled back (per-event observer filter,
   the double catch-up race) — each reattempt now has a deterministic oracle.

**Done when:** the integration suite passes with the settling buffer set to zero.

### Task 2.2 — Release train with a soak
**Traces to:** 303 patch releases; 8 patches the day after 15.0.0; 16.12.0's next-day shim
(16.13.2) itself never ran (#3615); 16.13.4 produced three same-week production incident reports
(#3570, #3571, #3591).

**Implementation** (needs a **cadence decision from the user** first: e.g. 1–2 releases/day):
1. Decouple merge from release in `publish.yml`: merges accumulate; a scheduled (or manually
   dispatched) release job takes the current main, runs the full OOP matrix + the Task-0.1 boot
   gates + Task-0.2 consumer smoke as a **soak**, then publishes. Use the GitHub **prerelease
   flag** for the RC during the soak and promote it on success — the channel is completely free
   (zero prereleases in the last 400 releases, see §1), and it gives consumers an explicit
   stable/RC distinction for the first time.
2. Add a small always-on canary app (a minimal event-sourced consumer exercising append →
   projection → PII → replay) run against the RC as part of the soak.
3. Keep an escape hatch: a `hotfix` label or manual dispatch that releases immediately (still
   through the boot/smoke gates).

**Done when:** patch share of releases is below 40% for a full calendar month.

### Task 2.3 — Close-the-class policy
**Traces to:** the mechanism that already bent the curve — 45 CHR analyzer diagnostics, and the
2026-H2 idiom of landing every regression fix with a "pinning spec" because the broken behavior
"had no coverage at all, which is how the flip shipped silently" (`092584131`, `9f0d73062`,
`98b123087`).

**Implementation**
1. Add the policy to `.ai/rules/pull-requests.md`: *a regression fix is not complete until its
   **class** is encoded somewhere a machine checks — analyzer, contract spec (Task 1.1), CI gate,
   or pipeline assertion — or the PR states which existing check covers it.*
2. **Gotcha:** do not add a checkbox to `.github/pull_request_template.md` — PR descriptions are
   published verbatim as release notes in this repo. Enforce via the `.ai` rule (agents follow
   encoded rules) + review, or a bot check on fix-PRs, not via template pollution.

**Done when:** a month of merged regression fixes all either add a machine check or name the
existing one.

### Task 2.4 — Regression-tax dashboard
**Implementation**
1. Scheduled monthly workflow computing, for the trailing month: revert count
   (`git rev-list --count -i --grep=revert`), patch share of tags, regression-language issue rate
   (`gh` search), fix/revert-prefixed commit share.
2. Publish as a markdown file the workflow commits (e.g. `Metrics/regression-tax.md`) or a
   pinned issue it updates. Include the targets: **volume flat or up · fix-share < 10% · patch
   share < 40% · reverts trending to the pre-2026 baseline (~1/month)**.
3. Baseline table to seed it (from the analysis): fix-share by month Sep'25→Aug'26:
   1.8, 12.4, 20.7, 15.6, 12.9, 19.9, 16.5, 24.5, 26.3, 24.5, 10.6, 9.1 (%).

**Done when:** the numbers publish automatically and the Status Board below gets a monthly
review row.

**As built.** `.github/workflows/regression-tax.yml` runs on the 1st at 07:00 UTC, measures the
month that just ended with `.github/scripts/regression-tax.py`, and commits
`Metrics/regression-tax.md` — verdicts first, then the whole series beside the seeded baseline in
`Metrics/fix-share-baseline.txt`. A committed file over a pinned issue: the repository already
commits generated statistics, a file diffs (each month is a reviewable delta), and it needs no
issue number to track or write scope on issues. A source that cannot be read is written into the
dashboard as its reason and fails the run; it is never reported as a zero.

Two definitions had to be refined to reproduce the published series, both recorded in
`.github/scripts/regression_tax_metrics.py`:

- **Fix-share matches the stem, not the word.** 1,761 subjects in this repository begin *"Fixing"*.
  Reading step 1's "fix/revert-prefixed" literally (`^(Fix|Revert)\b`) yields 3.8% for Nov'25
  against a published 20.7%, and a mean absolute error of 7.9 points across the eleven complete
  months; the stem match brings that to 1.4 points.
- **Reverts are counted by subject.** `git rev-list -i --grep=revert` searches the whole message
  and returns 84 over Sep'25→Aug'26, but 39 of those merely *mention* reverting (a dependency bump
  "and revert the alias workaround"). Subject-stem matching gives 45 in that window and 10 in the
  prior year — 0.8/month, which is the ~1/month the target is set against.

Unreconciled: the plan's 90-of-304 regression-language issues. The five listed phrases over the
same window give 74 of 312, and no wider phrasing tried reaches 90. The code's definition is now
the authority; the published figure cannot be re-derived from the phrases as stated.

Every figure in the three paragraphs above was measured on 2026-08-18. The three that span an
open window — the 84, the 74 and the 312 — keep moving until Aug'26 closes; the per-month series
in `Metrics/regression-tax.md` is the stable record.

---

## 6. Known open sores (fix opportunistically, they're already issues)

Not program tasks, but a session working nearby should know:
- **Proto generation is still churning** — the Aug 15 exit-code fix + regen was reverted within
  hours (`d5ae2df47`, `3c65086cb`). Task 0.3 must land the exit-code part without the regen part
  if the regen is what broke.
- Still-open severe issues at time of writing: #3532/#3345 (nested ChildrenFrom never projected),
  #3674 (Key Vault erasure soft-deletes instead of purging — GDPR-relevant), #3701 (reactor
  replay reports success but does nothing).
- Empty `[ChildrenFrom]` collections being **absent** (not `[]`) is **by design** (replay race) —
  do not "fix" the sink to write `[]`.

---

## 7. Status Board

Update this table in the same PR as the work. `blocked-on-user` = needs a settings change or
decision only the user can make.

| Task | Title | Status | PR | Notes |
|---|---|---|---|---|
| 0.1 | Boot gate on published images | **done** | #3735, #3758 | all 4 variants; the gate itself shipped broken (missing TLS + token certs) and blocked 8 publishes — fixed in #3758 |
| 0.2 | Consumer smoke for packages | **done** | #3755 | starts packed Chronicle beside published Arc before push; runs, does not merely restore |
| 0.3 | Silent-success sweep | **done** | #3737, #3762 | zero-test guards, solution-membership gate, generators exit non-zero. #3734 (proto regen) still held for Einar |
| 0.4 | Path-filter audit | **done** | #3737, #3761 | 13 of 17 required paths were uncovered; all closed by widening, contract asserted in CI |
| 0.5 | Red-workflow alarm | **done** | #3736 | advisory; would flag benchmarks + integration.yml today |
| 0.6 | Autofix bots → suggesters | todo | | CODEOWNERS in-repo; bot permission is blocked-on-user |
| 0.7 | Release hygiene on GitHub Releases | partial | | 12 broken releases warned retroactively; notes-quality gate = §8-g; NuGet deprecation blocked-on-user |
| 1.1 | Shared storage contract suite | **done (core)** | #3749–#3752, #3754 | 9 cases × 3 sinks; found D-8, D-9, ChildRemovedFromAll. Remaining: constraints + key storage suites |
| 1.2 | Harness convergence (finish) | partial | #3759, #3760 | WaitForCompletion no longer lies (merged); compliance wiring open. ReadModelScenario runs NO compliance — feature, not wiring |
| 1.3 | Hot-core OOP gate + CODEOWNERS | todo | | shares CODEOWNERS with 0.6 |
| 1.4 | History-integration guards | partial | #3763 | spec-deletion tripwire merged; branch-protection/merge-queue still blocked-on-user (#439) |
| 2.1 | Deterministic completion signals | todo | | design doc first; after 1.2/1.3 |
| 2.2 | Release train + soak | todo | | blocked-on-user: cadence decision; use the unused prerelease flag as the RC channel |
| 2.3 | Close-the-class policy | todo | | `.ai` rule, NOT the PR template (§5 gotcha) |
| 2.4 | Regression-tax dashboard | in review | | monthly review = the 1st-of-month commit to `Metrics/regression-tax.md`, seeded to Jul'26: passes volume and fix-share (9.9%), fails patch share (56.8%) and reverts (5) |
| 8.a | Regenerate-and-diff gate | todo | | |
| 8.b | Public-API zero-coverage ratchet | todo | | |
| 8.c | Kernel-facing analysis | todo | | the CHR family is consumer-only today |
| 8.d | Timing-coupling ratchet + rule | **done** | #3765 | 67 governing / 25 legitimate sleeps; unannotated baseline entries are 2.1 work |
| 8.e | Silent-stub sibling comparison | todo | | |
| 8.f | Path-filter completeness meta-check | **done** | #3761 | |
| 8.g | PR-body and commit lint | todo | | heading allowlist, not a denylist |
| 8.h | SQL provider on PR runs | in review | #3764 | SQLite added; blocked on 2 live non-Mongo regressions on main |

**Session log — 2026-08-18.** Seventeen pull requests merged: the whole of Phase 0 except 0.6 and
the rest of 0.7, the core of 1.1, half of 1.2 and 1.4, and audit items 8.d and 8.f. Two things worth
carrying forward. First, **the boot gate shipped broken and blocked eight publishes** — it started the
production image without the TLS and token-encryption certificates that image deliberately requires,
so it could never have passed; it had been verified against a locally built image rather than a
published one. A gate is not proven by passing, only by failing when it should. Second, **merging
eleven pull requests in two minutes raced the release job**: every run read the same latest version
and tried to create it, and the losers got a 403 from secondary rate limiting. Nothing was lost, but
it is the argument for 2.2 in miniature, and #3756 serializes it.

**Held deliberately, not forgotten:** #3734 and #3748 touch the proto surface and the gRPC gate while
Einar is mid-investigation there; #3760 makes real schema validation run in-process, which can surface
failures in downstream and Arc specs; #3756 changes release behavior. Each wants a human to choose the
moment.

**Suggested order for a fresh session:** 0.7-step-1 (retroactive warnings — protects users
today, zero build risk) → 0.3 → 0.1 → 0.4 → 0.5 → 0.2 → 0.6 → rest of 0.7, then 1.1 → 1.3 →
1.4 → 1.2, then Phase 2. Rationale: 0.3 and 0.1 close the classes behind every *broken release*;
1.1 is the highest-leverage correctness item and 1.2 builds on it.

---

## 8. Enforcement-audit additions (2026-08-17)

Two audits mapped every corpus invariant and incident category to its actual enforcement. The
findings below are the prose-only gaps worth closing, in leverage order. The single structural
headline: **the 45 CHR analyzer diagnostics ship to *consumers* and are applied to almost nothing
in this repo** — the kernel, where categories G/E/C (40 of 73 incidents) live, has no
Chronicle-specific analysis at all.

| # | Task | Why (evidence) | Done when |
|---|---|---|---|
| **8.a** | **Regenerate-and-diff gate** for generated output | 154 `// @generated by Cratis` files and 6 committed `.proto` files; no `git diff --exit-code` exists anywhere in CI, and every CI build passes `-p:DisableProxyGenerator=true`, so CI has never produced a proxy to compare. Catches hand-edits *and* staleness with one predicate | A job regenerates protos + proxies and fails on any diff; expect one determinism-stabilization pass |
| **8.b** | **Public-API zero-coverage ratchet** | 20 of 35 public types in `Cratis.Chronicle.Testing` are named by none of the 4,086 spec files — including the shipped `AppendResult` assertion helpers. Coverage is collected and charted but gates nothing | Tier 1: a committed baseline; a PR adding an uncovered public type fails. Tier 2: join the existing cobertura report instead of name-grep |
| **8.c** | **Point analysis at the kernel** | The CHR family is consumer-only (one sample project opts in). Start with a solution-wide CI tool using `Microsoft.CodeAnalysis.Workspaces`, hosting 8.e plus a narrow sentinel analyzer (`is null` on a type declaring `Unspecified`/`All`/`Default`/`NotSet`) | The sentinel check and 8.e run over `Chronicle.slnx` in CI |
| **8.d** | **Timing-coupling ratchet + the missing rule** | ~91–114 outcome-governing sleeps in specs/integration, and **no rule anywhere forbids sleeping in a spec**. This is category G, the largest bucket | `specs.csharp.md` says specs await facts, not durations; a committed per-file baseline that PRs may only lower |
| **8.e** | **Silent-stub sibling comparison** | "A stub that silently succeeds is a bug" is prose. Blanket detection false-positives on genuine no-ops; the sharp predicate is *another implementation of the same member has a real body* — which is also a contract-drift detector | Flags a `Task.CompletedTask`-only member whose sibling implementations do real work, with an `[IntentionalNoOp("reason")]` escape hatch |
| **8.f** | **Path-filter completeness meta-check** | `.globalconfig`, `.editorconfig`, `global.json`, `Directory.Build.targets`, `nuget.config`, `Docker/**` and non-`.cs` files under `Source/**` still trigger no build. A committed required-paths list makes the next "speed tweak" state what it drops | The workflow's `paths:` is asserted a superset of `build-affecting-paths.txt` |
| **8.g** | **PR-body and commit lint** | PR bodies publish verbatim as release notes; v16.13.4 shipped an empty `## Test plan` heading. Only the semver label is gated today | Heading allowlist (never a denylist), no closing keywords, no agent transcripts, commit-subject rules. **Not** a PR-template checkbox (§5 gotcha) |
| **8.h** | **One SQL provider on PR runs** | `generate-integration-matrix.py` defaults to mongodb; sqlite/postgres/mssql are schedule-only. This is exactly why a MongoDB-shaped assumption merged green and broke the nightly for six nights | A non-Mongo backend runs on pull requests (sqlite is container-free) |

Already covered elsewhere and deliberately **not** re-proposed: nullable event properties
(CHR0012), event-is-a-record (CHR0021), XML docs on public members (SA1600, active), docs code
examples (`client-snippets` compiles them with warnings-as-errors), and spec folder conventions
(measured 0 violations in 4,086 files — folded into the membership gate at zero cost rather than
built standalone).
