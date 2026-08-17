# The Chronicle Reliability Program

Root-cause response to the last twelve months of regressions (2025-08-17 → 2026-08-17).
Every item is a machine-checked gate with a testable definition of done — the program must
not live in prose, because prose erodes (a pipeline sat red for three months; a path-filter
speed tweak silently reopened a coverage hole).

## The diagnosis in one paragraph

Commit volume grew 2.4× (7,288 vs 3,069) while reverts grew 7× (79 vs 11); 303 of 394
releases were patches, half of sampled patches fixed a regression introduced days earlier,
and 60 of 90 regression-language issues say "silently". Classified against 73 incidents from
git history, the causes are overwhelmingly properties of the *verification system*, not of
authorship: fix-induced regressions in the timing-coupled observer/projection core (20),
concurrency/stale-state invisible to in-process specs (15), cross-cutting changes missing a
consumer (9), CI blind spots (8), vacuous verification + in-memory/engine contract drift (10),
history-integration drift from parallel agent branches (~4), autofix bots (3). Proof the
lever works: when the June–July guardrails landed (`.ai` corpus, `framework.md` fidelity
rules, 45 CHR analyzers, harness convergence), the fix/revert commit share halved — from 26%
to ~10% — while volume stayed near peak. Harden the oracle; keep the volume.

## Phase 0 — this week: no broken release can ship (six one-day gates)

| # | Gate | Traces to | Done when |
|---|---|---|---|
| 0.1 | **Boot gate**: `publish.yml` starts every image variant (production *and* development) and requires healthy before push | exec-bit 15.37.0–15.38.2; dev images broken since 16.33.1; #3250 | Reintroducing the exec-bit bug fails the publish job |
| 0.2 | **Consumer smoke**: restore just-packed NuGet/npm artifacts into a minimal consumer (incl. one spec using `Cratis.Chronicle.Testing`) and build before publish | #3598 — Testing × Screenplay 2.0 broke five release lines | The #3598 skew, replayed, blocks publish |
| 0.3 | **No silent success**: generators exit non-zero on failure; every CI-executed tool compiled by a build gate; every test job asserts executed-spec-count > 0 | proto gen failed 22/23 pkgs, exited 0 for months; reserved-fields generator in no solution; integration job once ran zero specs | A deliberate generator failure and an empty test run both turn CI red |
| 0.4 | **Path-filter audit**: triggers cover `Directory.Packages.props`, `Benchmarks/**`, `Docker/**`, `.github/workflows/**`; one documented filter list | unbuilt Fundamentals bump merged; benchmarks broke green; exec-bit fix couldn't cut its own release | A PR touching only `Directory.Packages.props` runs the full build |
| 0.5 | **Red-workflow alarm**: N≥3 consecutive failures auto-files an issue; unfixed workflows get deleted | prerelease pipeline: 319 consecutive failures, 3 months, no reaction | Three red runs of any workflow produce an issue automatically |
| 0.6 | **Autofix bots demoted to suggesters**: PR-only, full warnings-as-errors Release gate, never on hot-core paths | two autofix build breaks; mechanical churn of hot kernel files | No bot has direct-commit; hot-core paths carry CODEOWNERS |
| 0.7 | **Release hygiene**: retroactively mark known-broken GitHub releases with warnings + pointers (15.37.0–15.38.2 unbootable images; 16.15.0–16.19.2 broken Testing pkg); hotfixes must edit the superseded release; notes-quality gate strips template leakage | zero of 400 releases are prereleases; broken releases sit unmarked and pullable; "## Test plan" leaked into v16.13.4's public notes | Every confirmed-broken release opens with a warning; a body containing template leftovers is rejected |

## Phase 1 — weeks 2–4: green must mean correct

| # | Work | Traces to | Done when |
|---|---|---|---|
| 1.1 | **Shared storage contract suite**: write each storage contract once as abstract specs; run against InMemory, MongoDB, SQL (extend to sinks). Today the three spec projects share nothing | sentinel-filter empty reads; generation stamping; SQL-sink PII stall; SQL PropertiesChanged drops | The sentinel bug, reintroduced, fails the in-memory run of the shared suite |
| 1.2 | **Finish harness convergence** onto the kernel's real in-memory storage; close the known `ReadModelScenario` fidelity list (PII, child removal, child-key resolution, string-keyed joins, reducer initial state) | the harness-fidelity family — specs green, production wrong; agents iterate until a divergent harness passes | Every fidelity-list item has a spec that fails against the pre-convergence harness |
| 1.3 | **Contain the hot core**: PRs touching `Observer.Handling`, `KeyResolvers`, `AppendedEventsQueue`, `Changeset`, compliance/PII require the full OOP integration matrix + CODEOWNERS | the join sagas; the observer-filter rollback that reinstated a known bug; failed-partition churn | A PR touching `KeyResolvers.cs` cannot merge without a green OOP matrix |
| 1.4 | **History-integration guards**: merge queue (up to date with main); tripwire failing PRs that delete spec files without deleting the code under spec; agent worktree freshness check | stale worktree silently reverted merged compliance hardening (green CI — specs died with the code); merge dropped SQL OpenIddict; rebase bricked dev images | Replaying the stale-worktree overwrite is blocked by the tripwire |

## Phase 2 — this quarter: dismantle the regression factory

| # | Work | Traces to | Done when |
|---|---|---|---|
| 2.1 | **Deterministic completion signals** replace the implicit 50ms settling buffer in the observer/projection core (explicit catch-up/consolidation acknowledgements) | category G — 20/73 incidents; two rollbacks deliberately reinstated known bugs | The integration suite passes with the settling buffer set to zero |
| 2.2 | **Release train with soak**: merges continuous; 1–2 releases/day; RC soaks against the OOP matrix + a canary app before publish | 303 patch releases; median hotfix 1–2 days — users are the canary today | Patch share of releases below 40% for a full month |
| 2.3 | **Close-the-class policy**: a regression isn't closed until its class is machine-checked (analyzer, contract spec, CI gate, pipeline assertion); PR-template checkbox + review rule | the mechanism that already bent the curve (45 CHR diagnostics, pinning specs) | Every regression fix in a month adds a check or states why the class is covered |
| 2.4 | **Regression-tax dashboard**: monthly automated publish of revert count, patch share, regression-language issue rate, fix/revert commit share | this report had to be assembled by hand | The numbers publish automatically; the program is reviewed against them monthly |

## Targets

Volume flat or up · fix/revert commit share < 10% and falling · patch share < 40% ·
revert count trending toward the pre-2026 baseline.

## Execution

The detailed, pick-up-and-work version of this program — per-task implementation guidance,
repo-specific gotchas, verification steps, and the live status board — is
**`PLAN-reliability-program.md`** at the repo root. That file is the working document; this one
is the summary.

## Provenance

Full analysis with charts, the 73-incident taxonomy, the top-15 severe user-facing
regressions, hot-area churn table, and fix→regression chains: see the published report
("The Regression Ledger"). Data: `git rev-list`/log over the window, 304 GitHub issues,
14 sampled patch releases, commit-author and Co-Authored-By attribution.
