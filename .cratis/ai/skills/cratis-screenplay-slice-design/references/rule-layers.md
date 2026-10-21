<!-- cratis-ai-managed: skills/cratis-screenplay-slice-design/references/rule-layers.md -->
# Rule layers

Put each rule in the lowest layer where it is true for every caller. Descriptions reach rendered
code only as XML doc comments (Stage#178): a rule that exists only in prose is unenforced in rendered scope.

| Rule is about | Layer | Notes |
|---|---|---|
| format or meaning of a value, true wherever used | `concept` `validate` | travels with every use; a value some use must accept unchecked is a different concept. One rejection spec per concept (through one command), not per use |
| a predicate that is not a comparison (checksum, registry format) | `rule <Name>` on the property or concept | design mode; blocks V3 (PLAY0268) |
| this command's own inputs | command `validate` / `require` | a rule that applies only sometimes is an implication: `require a == false or b > c` |
| who may ask | `authorize` with a policy (role, claim, authenticated) | enclosing gates combine with AND and run first. Gates every command and query in a feature shares go on the feature or module so a new path inherits them |
| whose instance | policy `claim "x" matches subject` (the command identifier) or `matches <command property>` | executable; renders for text, Uuid, Date and DateTime subjects; an authorized command needs exactly one identifier (STAGE-ESM-015 otherwise) |
| uniqueness, once-only facts | `constraint` (`unique ... on`, `unique event`) | append-time; the constraint name is its identity |
| stored state (record exists, balance, current status) | `reads <View>` + `require <expr> message "..."` | stated intent, **NOT enforced in the model today** (PLAY0268/0271 at binding are expected in design mode). Name the target: Arc `[ProtectedDecision]` + `DecisionRead<T>` (Requires Arc 22.39.0 or later), Chronicle DCB (`concurrency` scope), or a constraint |

An unguarded materialized read is unsafe for a protected decision; a guarded read is the
supported route. Availability: `[ProtectedDecision]` and `DecisionRead<T>` exist in
the Arc/Chronicle package set Stage scaffolds (see the Stage skill's `references/rendered-application.md`), but Stage does not emit
protected decisions. Gap-fill must use an admitted projection/key shape and active
unit-of-work enrollment; do not edit managed dependencies. Keep the `.play` rule
NOT enforced until its target enforcement is separately verified. Forbidden: caller-supplied copies of state in `require`, boolean attestation
inputs (`confirmsX == true`), rules in `handler` / implementation-hint prose.

## Authorization is executable

Persona Does/Reads/Cannot text is intent. Every Cannot line resolves to an `authorize` gate
(policy or ownership claim) plus a `then denied` spec using `given caller as <Persona>` or an explicit
caller for the effective gate; otherwise record a gap. Every command and query under an
inherited module/feature `authorize` gets its own `then denied` spec. Ownership gates need a
second caller with another claim value.

## Secrets and personal data

Command property values are, by default, recorded in the causation chain of the events they
produce, so treat them as permanent. Chronicle can omit causation properties
(`CausationPropertyRetention.Omit`); Stage-rendered apps pin the Chronicle release listed in the Stage skill's `references/rendered-application.md` (not necessarily the latest).

Requires Chronicle 19.20.0 or later for `CausationPropertyRetention.Omit`.
Verify the actual auditing and retention configuration before relying on omission, and never
audit secrets. Classify personal values as bare `pii` and operational secrets as
`secret` on their concepts, never the identifier concept. Use a surrogate identity
and protected payload (PLAY0515). Processing use/basis/retention is a `purpose`,
not a reason; see command-surface `references/compliance.md`. Bearer tokens, magic links and signed URLs are never facts (record a keyed hash or
reference).
