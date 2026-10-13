<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/traps.md -->
# Traps (one line each, with the fix)

Current pins: `versions.md`. Unless explicitly
updated below, [probed]/[source] rows retain their historical v4.66.0/CLI 3.28.2
scope; negative capability claims there are not current verdicts. Current routes
bind as v8, v7 reaches Stage planning, reference tests have CLI/MCP routes, and
exact numbers are documented in the grammar. Compiler syntax: `cheat-sheet.md`.

1. Clean V1 is not executable: binding errors appear through MCP (V2), `render` or `cratis screenplay validate --executable` (bundled compiler, writes nothing). Neither `screenplay <folder>` nor plain `cratis screenplay validate` binds. [probed]
2. The Screenplay samples validate clean but are not all executable; copying their idioms (context-filtered queries, enum/identity compliance, `today`, `reads`, `$causedBy`, templates, performers) can block V3. Caller-supplied collection equality filters bind since 4.143.0 (ESM v14); read read-surface `references/equality-filters.md`. [historical probes; current filter source checked]
3. Know the target mode (design/executable/renderable) before writing; state it.
4. Plain `produces` without `for` means allocation in the ESM but "the identifier" in rendered code. Always write `for <identifier>` on a plain `produces <Name>`; inline `produces event <Name>` omits `for` and defaults to the identifier. [PLAY0478 probed]
5. PLAY0029 is a warning: a misspelled keyword (or `type` inside a slice) drops the construct silently. Use `--warnings-as-errors`. [probed]
6. Validate the folder: `cratis screenplay validate file.play` ignores imports and reports false unknown-name warnings (cli#244, open); the standalone tool follows them. [probed on 3.28.2 and 4.66.0]
7. Current V1 warns on absent projection/children/nested targets (PLAY0514) and unknown removal/capture events (PLAY0166). Opt-in field-origins checks find unmapped declared fields (PLAY0534) where coverage is known; still walk lineage for skipped/opaque shapes.
8. Severity does not soften a rule: `severity warning` still rejects the command.
9. Authorization (module AND feature AND construct) runs before validation; unauthorized is `then denied`, never `then error`.
10. Without a declared actor an invocation is caller-less. At 4.125.0 declare reaction `runs as system role "<Role>"` (ESM v10), keep its gate, and reference-test the trusted path. The historical CLI 3.41.0/4.114.0 parser rejected this syntax (PLAY0137); previous CLI 3.46.1 refused v10 at its version gate; the pinned CLI/Stage pair (`versions.md`) renders role/authenticated reaction identity; claim-reading/opaque system gates still refuse STAGE-ESM-034. Arc gap-fill uses `[ExecuteCommandsAsSystem]` for returned commands, not a fictional `given caller`.
11. A reaction `produces` without `for` lands on the trigger's event source; clock and application triggers need an explicit `for`.
12. Explicit trailing `key` marks replace inference (never `identifier`); multipart read-model keys remain PLAY0268 (#599). Without marks, single-instance queries in the owning slice must agree on `by xId XId`, where `xId` is a read-model property. Only with no such identifier and an admitted list query does `*Id` inference run (collection-key exclusions: read-surface `references/named-keys.md`); queryless views need explicit `key`. The identifier equals the projection's effective key (table in slice-design `references/read-model-design.md`; full rule, including Stage's narrower admission and `STAGE-ESM-017`, in `cratis-stage-rendering-and-sandbox` `references/admission.md`). Never mark a read-model property `identifier`. (Without explicit `key`, PLAY0268 when identity inference is ambiguous or absent; PLAY0351 when a `given`/`then` readmodel block omits that property.)
13. Projection `key` routes only on `from` (projection-level: PLAY0381). `all` is per event source. Joins never create instances and match the joined event's source id. [probed]
14. AutoMap is on by default: same-named event properties fill read-model properties silently (scoped: case-insensitive, same type only; flat-bound: exact name, no type check, so a type mismatch invalidates the model). Use `no automap` for control.
15. `given readmodel` bypasses the projection; prove a projection with `given` events + `when append`/command + `then readmodel`/`then query`.
16. PLAY0285/0286/0287 are static spec checks: do not edit expectations blindly; PLAY0285 had a cascade false positive on the compiler bundled in cratis before 3.28.2 (4.60.1, cli#242); gone in 4.66.0 and cratis 3.28.2. [probed]
17. `null` in specs only for optional read-model properties (PLAY0350); omit the property or record a gap; whether an optional detail is really a separate fact is a review question, not the fix for this code.
18. `then` events are exhaustive and ordered (use `then events in any order` for order only). From ESM v6 they include reaction cascades; after `when append` they list only what followed the append.
19. `reads` without `by` means a singleton view at Screenplay 4.66.0 (decision 0017 will change it); `reads` never binds (PLAY0271), and neither does a command `concurrency` block (PLAY0271, error). [probed, source]
20. Declare each event once, in its producing slice; renaming a persisted event needs `id "OldName"`; renaming a constraint discards its index.
21. Quoted `import "x/*.play"` = my files; unquoted `import Ctx.Event` = another context's contract (does not bind: PLAY0268). [probed]
22. Construct keywords are closed (`aggregate`, `saga`, `workflow` do not exist): PLAY0029 or PLAY0001/0022/0024.
23. `@` escapes are mandatory for `tag`, `authorize`, `produces`, `reads`, `file`, `validate`, `sequence`, `occurred`, `causedBy`, `namespace`, `correlation`, `causation`, and projection `key`/`parent`/`with` as names.
24. Canonicalizing edits can drop unplaceable comments (PLAY0288; inspect `droppedCommentCount`); keep durable rationale in fenced Markdown `documentation`, not `//`, and summaries in `description`. Documentation survives canonical printing but changes neither executable bytes nor `modelRevision`; it is not rendered code.
25. MCP writes: propose, review, approval, apply by the identity owner (the session that owns the MCP connection); other subagents return edit requests; proposals are connection-local, at most 16 (`cratis-screenplay-model-authoring`).
26. Operations/systems and `numbers exact` source mode remain unadmitted. Qualifying retained non-route Double literals instead select published v11 at 4.135.0. Sources/streams/routes bind as v8, public/direction forms v9, identity/overrides/filters v10. The pinned CLI (`versions.md`) reaches Stage construct checks through v15; supported equality filters and strategies render. See `versions.md`.
27. Translate crosses the public boundary: inbound foreign/data to private local facts; outbound private facts to one local public type. Public metadata needs explicit direction; a quoted origin is an unverified store name, not a path. Commands never publish public events. In-app private follow-up is Automation; see public-events-and-direction in the method skill.
28. Personas drive the board: a persona must list every policy on the path, including module-level `IsAuthenticated`.
29. A sample's source check is not execution. Run selected admitted specifications through `screenplay test`/MCP; given==when clock still fires no scheduled occurrence.
30. Claims compare with `matches` (`claim "dept" matches "Finance"`); `==` is PLAY0120. Conditions with `contains`/`starts with` compile but do not bind. [probed]
31. One `every`/`all` block per projection level for V3; `$eventContext.occurred.Week` does not bind. [probed]
32. Concept rules: `min`/`max`/`length ==`, not `length 3 to 10` (PLAY0141). [probed]
33. Which compiler matters: cratis before 3.28.2 bundle Screenplay 4.60.1, which rejects ESM v6 (Automation/Translate) and reports a false PLAY0285 on cascades; cratis 3.28.2 bundles 4.66.0 and behaves like the standalone tool. Check `cratis --version` and name the tool in every verdict. [probed on both tools]

34. A `from <Event> key <property>` routes only if every such event carries that property: an event that frees or updates a row keyed by another stream's id must carry that id in its payload, or the row never changes. Whether the compiler reports a missing key property is unverified [to probe]; check by hand.
35. `description`/`documentation` are authoring metadata: board, MCP and Prologue show them; `cratis render` emits them only as XML doc comments (Stage#178 closed; invalid XML characters are CLI-RENDER-005), never as behavior. A rule only in prose is unenforced in rendered code; make it a rule layer, a spec or a recorded target requirement.
36. One unadmitted construct (operations, `numbers exact`, composite view keys, refusal/redelivery) blocks whole-model binding. Routing is admitted v8, unlike composite view keys (#599); current CLI admits through v15 to construct checks. Overrides replace the whole route, filters never match unrouted facts. Do not remove routing or authorization to render a model.
37. Personas remain outside ESM bytes, but 4.127.0 specifications accept `given caller as <Persona>`: one authenticated deterministic policy witness, not every matching caller. Inspect the chosen `or` branch; use explicit callers when synthesis is refused.
38. A rule that depends on stored state is `reads <View>` + `require <expr> message "..."` (stated intent; PLAY0268/0271 at binding are expected in design mode). The slice `description` must mark it NOT enforced in the model today (Screenplay#129) and name the target: Arc `[ProtectedDecision]` + `DecisionRead<T>` (Arc v22.39.0 and later; available to gap-fill on Stage 4.51.3's Arc 22.50.5/Chronicle 19.32.0, not emitted protection), Chronicle DCB (`concurrency` scope), or a constraint where one fits. Forbidden: caller-supplied copies of state in `require`, boolean attestation inputs (`confirmsX == true`), rules hidden in `handler`/implementation-hint prose. An unguarded materialized read is unsafe for a protected decision.

Identity/compliance traps checked at v4.127.0 (`identity.md`, `concepts.md`, `events.md`,
`purposes.md`, `diagnostics.md`):
- `$identity` built-ins alias `$context.identity`; declared detail reads are PLAY0268,
  while an identity block alone does not block readiness. `scoped to` is opaque,
  not a detail reference. Query sources need keyed single compatible results and
  token-only keys/gates that do not depend on details.
- `pii personal` repeats one marker (PLAY0653). Legacy markers are accepted but
  deprecated (PLAY0565); repairs preserve notes, never invent legal content.
- Event `subject` is report-only (PLAY0270), not emitted C# `[Subject]` or runtime
  lineage. Keep stream identity, data subject, encryption scope and policy subject apart.
- Purpose metadata/checks/records declare processing, not lawfulness or retention.
- PLAY0504 is an error; stream-id text is nonempty well-formed NFC (refuse, never
  normalize), Double integer ids are within ±(2^53−1); canonical UUID comparison
  ignores case. Protected concepts cannot be stream ids/parts/mapping sources (PLAY0515).
- Run `--check privilege` on elevated event-trigger paths; opaque producers are
  unknown, not safe. Arc needs `[ExecuteCommandsAsSystem]` to supply a principal.

The SKILL.md top 12: 1, 4, 5, 6, 7, 10, 12, 13, 17, 18, 20, 21 (+ mode/version notes there). Traps 39 to 44 are tool traps added at the 4.66.0 pin.
39. A handler never binds: PLAY0268 "Command '<n>' handler requires a constrained implementation attachment", with a file, a fence, `implementation` or `hint` alike. `implementation` and `hint` are accepted on a command handler, an operation execute/compensate phase and (from 4.65.0) a command property named rule, and are rejected on concept rules, built-in property rules and whole-command `require`/`validate` bodies; on a handler they are authoring intent only (no execution, lock or confirmation). A handler slice is gap-fill, not a customization of a rendered slice. [source, probed]
40. `numbers exact` (the source mode) parses but never binds at any release through 4.139.0 (exact lowering of qualifying Double-mode literals is separate: v11) (PLAY0268; "not admitted by any supported executable model (ESM) version yet") and is PLAY0001 on older compilers; it is absent from `grammar.md`. Never use it in a model or example meant to execute. [probed]
41. MCP roots bug (4.63.1 and earlier, including the 4.60.1 bundled by cratis before 3.28.2; not present in 4.66.0 or cratis 3.28.2, probed): a dynamic-root server answers the roots request with an `id: null` error right after `notifications/initialized`; hosts treat it as fatal. `open-workspace.path` does not help. Start with a fixed root (`screenplay mcp <folder>`, `cratis screenplay mcp` in a project with `.cratis/ai.json`) or use 4.63.2 or later (cratis 3.28.2 or later). [source, probed]
42. A symlinked model path is refused by the MCP server (macOS `/tmp` is a symlink); pass the physical path. Proposals are connection-local (at most 16); `apply` persists identities to `.screenplay/identities.json` and is not crash-atomic across files; never retry `apply` after `ApplyOutcomeUnknown`. [source]
43. `modelRevision` is canonical semantic (line positions and descriptions do not change it; a file move only if it changes logical placement or application identity) and, without `identities.json`, changes with the root folder name; it is not comparable with a render's `semanticRevision`. Source identity for verdicts is the commit plus the digest from the source-identity helper (`cratis-screenplay-modeling-lifecycle` `references/verdicts-and-modes.md` "Source identity"). [probed]
44. `cratis render` neither builds nor tests; `--name` is required for plain source; `.cratis-render/` is control state and is never staged; Stage 4.51.3 rendered apps pin Arc 22.50.5/Chronicle 19.32.0 (`[ProtectedDecision]` available to gap-fill, not automatically emitted). `cratis` file mode versus folder mode (trap 6) and a stale `cratis` on `PATH` (`which -a cratis`) both silently change what a verdict means. [source]
