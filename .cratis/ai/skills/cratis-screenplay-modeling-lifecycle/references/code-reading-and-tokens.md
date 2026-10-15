<!-- cratis-ai-managed: skills/cratis-screenplay-modeling-lifecycle/references/code-reading-and-tokens.md -->
# Code reading and token rules

Model-first work reads models, not application code. The rules below say when code is read and
how little output is printed. They govern reading and printing, never the model's content
(completeness over economy, `SKILL.md`).

## Rules C1-C7 with examples

| Rule | Allowed | Not allowed |
|---|---|---|
| C1 modeling (P1-P7) | existing `.play`, user documents, tickets, Screenplay docs | opening feature code to "see how it works" |
| C2 review | a locator such as `src/Billing/InvoiceService.cs:120-140` cited by a legacy packet row | browsing the repository to verify a hunch |
| C3 legacy | provider inventory; generated `.play` plus diagnostics; Prologue output; one handler to resolve a named evidence row, logged as `E7 <- RefundHandler.cs:44-61`; a bounded sample of an entry point the generator skipped | reading every controller before L1; treating "not seen in capture" as "does not exist" |
| C4 render | `Customizations/*`; the generated line a build error names; the declared extension contract and the generated types it touches | scrolling managed output; editing a managed file |
| C5 gap-fill | the code agent reads code for the non-renderable slice, with the slice and its specs as the contract | changing behavior the specs do not describe without recording it |
| C6 runtime | read-only queries against a running Chronicle system (`cratis-chronicle-cli-operations`) | reading code to guess runtime state; using Chronicle tooling on a non-Chronicle legacy app |
| C7 user | "read `PricingEngine.cs`" means that file, that scope | widening to the folder |

An explicit user request overrides the no-code rule for the named scope only. Sweeps over about
three files go to a read-only exploration agent where the harness has one, with a precise
question; it returns paths and file:line findings, never file dumps. Reread when a source changed
or evidence conflicts, and log why.

## Orientation order
1. Validate counts (compact output).
2. MCP summary of the application.
3. Scoped search and declaration details for the area in question.
4. Read only the owning `.play` file(s).

## Never read
Managed render output, `bin/`, `obj/`, Arc proxies, `.screenplay/identities.json`,
`.screenplay/pending.json`, syntax-view dumps, executable-model exports (extract the
`modelRevision` field only).

## Editing
Small text diffs to `.play`, validated per coherent batch and once at the final revision. Typed
operations for renames, repairs, inline-event extraction and parser-invalid documents. Edit
strategy and identity: `identity-and-edits.md`.

## Output discipline
- Diagnostics: code, location, one-line meaning. At most about 30 lines of tool output in a
  report; long build or test logs become counts plus failing names.
- Tool output and packets are compressed (counts, addresses, five verdict lines); the phase
  report is never shortened by omitting elements, mappings or evidence.
- Briefs carry paths, packets and addresses, not pasted content.

## Budgets (indicative, per feature: one state change, one state view, two or three specs)
Orientation about 5k input tokens; authoring about 2k output tokens; verification about 1k input
tokens of tool output. Exceeding is fine when justified in STATE.md. Never meet a budget by
dropping slices, events, rules or specifications.

## Context
File layout: `cratis-screenplay-event-modeling` "Choose a file layout". Hand off to a fresh
session from STATE.md at a milestone or when the context grows large; global session limits
still apply.
