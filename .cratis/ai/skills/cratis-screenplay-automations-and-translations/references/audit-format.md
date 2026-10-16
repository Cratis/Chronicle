<!-- cratis-ai-managed: skills/cratis-screenplay-automations-and-translations/references/audit-format.md -->
# Audit format

Use after any reaction, capture or translation was added or changed, and at the end of a review.
Enumerate every affected declaration with its location; name any scope you did not review. A
result is complete, open or blocked, never "looks fine". The exhaustive enumeration is the
technique: partial work stays visible and nothing is skipped because it looked redundant.

## Per automation (reaction)
| Declaration | Location | Trigger inputs traced | Condition | Effect and actor | Pending lifecycle | Repetition scope | Closing paths | Named cases | Result |
|---|---|---|---|---|---|---|---|---|---|

Result is `complete` only when every column is filled or says why it does not apply (for example
"direct effect: immediate, internal, cannot fail"). `open` names the unresolved decision and who
answers. `blocked` names the actual capability gap with intended behavior. For example,
the CLI/Stage pair (toolchain `references/renderable-subset.md`) admits through ESM v15 to construct checks; role/authenticated reaction identity
renders, while claim-reading/opaque system gates still refuse STAGE-ESM-034. Previous CLI 3.46.1 refused v10 at the version gate
with CLI-RENDER-004; do not report that historical refusal as the current gap.

## Per translation (capture or translator reaction)
| Source and type | Location | Correlation | Field dispositions | Target fields traced | Dedup and ordering | Trust boundary | Recovery owner | Named cases | Result |
|---|---|---|---|---|---|---|---|---|---|

## Per-chain questions (ask of every automation checked)
1. Does it have a pending-work view, or a stated reason a direct effect needs none (immediate,
   internal, cannot fail)? A "simple relay" is not exempt from the question.
2. Is a worker's pending view opened only by our own facts? An outside fact reaching a decision
   directly means the translation step is missing: split it.
3. Does a translation that holds real pending work have a completion or terminal disposition? An
   artificial queue that never closes conceals recovery; omit it if there is no pending work.
4. Does a worker after a translation add a distinct decision, obligation or effect (for example
   notifying a member), even if it reuses the translated identity? If it adds none and only
   restates the fact, project downstream views from the fact instead.
5. Does a worker's view have a closing path for its own result fact, and for cancellation and
   supersession?
6. Does the pending view avoid a status field (membership is the state)?
Record a missing required or referenced queue opening or closing fact as an unresolved dependency
(a direct effect with no queue is "not applicable", with the reason);
never skip it silently or call the chain complete.

## Filled example (from `todo-list-example.md`)
| Declaration | Check | Result |
|---|---|---|
| `CertificateSender` | trigger `CoursePassed` selects `enrolmentId`; `invokes SendCertificate`; actor recorded as an internal command not exposed (target requirement); repeat absorbed by `IssueOnce` | complete |
| queue `CertificatesToSend` | opens on `CoursePassed`, closes on `CertificateSent`; cancellation or supersession: none in scope (stated); specs open and close | complete |
| `CertificateRetrySweep` | reads the whole view; per-item fan-out needs code (Screenplay#286) | blocked |
| external send once | not provable in the model; idempotency key = enrolment id is a target requirement | open (owner: realization) |
| Unreviewed | `RecordCompletion` denial for a non-coordinator is specified; query authorization not in scope | stated |

Then the verdict line per `cratis-screenplay-toolchain`: tool and version on each of V1 to V4
(V4 uses standalone `screenplay test` or MCP `run-specifications`; report the
actual result, or a specific host/input limitation if not run).
