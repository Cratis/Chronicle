<!-- cratis-ai-managed: skills/cratis-screenplay-legacy-extraction/references/rule-modeling.md -->
# Modeling legacy rules truthfully

## Refusals
- Input or constraint rejection: declarative `validate`, `require` or constraint, plus a
  `then error "<exact legacy message>"` specification.
- Authorization refusal: `authorize` with a policy (role, claim, authenticated, or an
  ownership policy) plus `then denied`, with a synthesized persona caller or an explicit effective-gate fixture.
  Every command and query under an inherited module or feature `authorize` gets its own
  `then denied` spec. A role gate with no executable form is a recorded gap.
- Legacy HTTP status and check order (for example 409 before 403) are facts about the old
  system: record them in slice `documentation` and the evidence row, not in the model logic.

## State-dependent rules
A rule that depends on stored state (current status, existing records, balances) is written
as `reads <View>` plus `require` with an indented message. Opt eligible views into
application-root `command reads protected` (v25); test present/absent state and rule
refusal, and matching decide/commit conflicts. Read
[protected reads](../../cratis-screenplay-command-surface/references/protected-reads.md)
for admission and Stage's refusal. For legacy/refused shapes, mark the rule unenforced.
Name the invariant, the race it leaves open and the target realization:
`[ProtectedDecision]` + `DecisionRead<T>` (Arc 22.39.0 and later; available to gap-fill
in Stage's rendered package set, not automatically emitted), or a Chronicle
`concurrency` scope on the event source or stream, or a constraint where one fits. Forbidden: caller-supplied copies of state
in `require`, boolean inputs standing in for a rule (`confirmsX == true`), and rules hidden in
`handler`/`implementation hint` prose. Descriptions render only as XML doc comments, so a rule
that exists only in prose is unenforced; list it in LOSS.md too.

## Unique indexes
When the business meaning is clear, write `unique <prop> on <every event that sets the
value>` with `released by <removal event>` and a pinned `message`, and list the open
questions: collation and case, whether the value may be reused after release, soft-deleted
rows. A constraint covers only the events it names, so name them all. Use a bare `rule` only
when no constraint form fits, and record that as a loss. Purely technical keys stay out.

## Other defaults
- No nullable event properties by default; a required value or a separate event.
- Never PII on the event-source id; one data subject per event or stream.
- Tokens, magic links and signed URLs are not facts (a keyed hash or reference at most).
- Do not store clock-relative state (`IsOverdue`) in views; store the deadline.

## Personal data and secrets
Preserve the protection found in code with `pii` and `secret` and evidenced secret scope. Arc
recovery losses (SP0061-SP0063) need review (`generate-notes.md`). Deprecated marker repairs keep
legal notes intact and never guess a purpose or basis. Derive purpose, use and retention
declarations only from evidence or expert decisions; run `--check purposes` and read the processing
record as prompts, not a legal verdict. Keep hand-joined legacy ids scalar and never infer composite
parts by splitting text. Event `subject` is report-only; distinguish observed lineage from runtime
realization. See command-surface `references/compliance.md`.
