---
name: cratis-engineering-decision-record
description: Consult, author, accept, and supersede decision records in a Cratis repository's decisions/ folder. Use before an architectural, contract, scope, or cross-cutting change, when a ruling has been made that later work must obey, or when an accepted decision has to be replaced. Defer product documentation, session handovers, and work-item status to their own workflows.
license: LICENSE
---
<!-- cratis-ai-managed: skills/cratis-engineering-decision-record/SKILL.md -->

# Cratis decision records

A decision is a durable choice with a **decider** and a **date**. It is
documentation, not a work record: it lives in the repository's `decisions/`
folder and is reviewed like any other documentation. A handover may summarize a
decision; it never holds the only copy.

This skill owns the *procedure* — how to consult, author, accept, and supersede
a record. It does not decide what to decide, and it never grants acceptance.

## When you need this

- You are about to make an architectural, contract, scope, or cross-cutting
  change. Consult first: a decision you did not read still binds the change.
- A ruling was made — in review, in chat, in a meeting — that later work has to
  obey. Record it in the same turn, while the reasoning is still available.
- An accepted decision no longer holds and has to be replaced, narrowed, or
  qualified.
- Your change would contradict an accepted record. Stop: supersession or a human
  verdict comes first, never a workaround.

## When you do not

- **Session notes, plans, handovers, status boards.** Those are work records.
  They belong in the repository's ignored local working directory, never in
  `decisions/`.
- **Product or API documentation.** A record says what was chosen and why; the
  documentation says how the thing works. Use the documentation workflow.
- **A work item's status.** "Blocked on X" is a work item field, not a decision.
- **A reversible choice inside your own scope that nobody will re-litigate.**
  Make it and move on; see the significance test in step 2.
- **A decision this repository does not own.** Company-level and portfolio
  decisions live in the record set that owns them. Cite that id; do not copy the
  record into a repository that cannot supersede it.

## Steps

1. **List the records in force for the paths you are changing.** Read
   `decisions/`, keep the records whose `applies-to` matches a path you are
   about to touch and whose `status` is `accepted`, and order them newest first.
   Report the count — "0 records matched" and "3 matched, none contradicted" are
   different verdicts and must read differently.
2. **Cite what you relied on.** Name the ids on the work item, in the pull
   request body, and as a `Decision: <id>` commit trailer. A change that
   silently contradicts an accepted record is a defect even when the code is
   correct.
3. **Apply the significance test before writing anything.** Write a record only
   when at least one of these holds: someone will otherwise re-litigate the
   choice; it binds paths beyond the one you are changing; reversing it would
   cost real migration or rework; or it rejects an option a reasonable reader
   would reach for. If none holds, say so and make the change without a record.
4. **Pass the completeness gate, or open with `status: returned`.** A proposed
   record states the options considered *including the one not taken and why*,
   the default that applies if the question is never answered and what that
   default costs, the timeline the decision has to hold to, and what is in scope
   and out. A record missing any of the four is returned to its proposer for
   revision — `returned` is not a rejection.
5. **Write the verification criterion before acceptance, not after.** State the
   observable signal that will say the decision was actually carried out, as
   `Done when` and `Verify by`. A decision whose success cannot be observed
   cannot reach `stage: verified`.
6. **Open the record as `status: proposed`, `stage: none`, and regenerate the
   index.** A record the index does not list is a record the consult step in
   step 1 will never find.
7. **Accept by recording a resolved actor and a date.** Set `status: accepted`,
   `decided` to the date, and `decider` to a named person — never a role, a
   team, or a tool. Acceptance is a human verdict: draft it, do not grant it.
8. **Spawn the build work carrying the criterion verbatim.** The `Done when` and
   `Verify by` text written in step 5 travels onto the work item unchanged, so
   the thing that gets built is the thing that was decided.
9. **Move `stage` only on the evidence the next stage requires.** `none` →
   `implemented` when the change exists in the tree; `implemented` → `verified`
   only on a signal observed this time. Accepted is not implemented, and
   implemented is not verified.
10. **Supersede rather than rewrite.** Never edit an accepted record's decision
    text in place — that text is what people relied on. Correct a typo or add
    context under a dated banner that says what changed and why. Change the
    *choice* only with a new record.
11. **Point both ways and sweep the citations.** The new record names the one it
    replaces in `supersedes`; the replaced record's `status` becomes
    `superseded` and it gains a `superseded-by` pointer forward, with its
    original text preserved. Then find every work item, pull request body, and
    commit trailer citing the old id and point it at the new one. A reader
    arriving at either record must be able to reach the other.

The exact front-matter fields, the closed value sets, and the index shape are in
[record-format.md](references/record-format.md).

## What breaks

- **A role in the `decider` field.** "The architecture team decided" names
  nobody who can be asked what they meant or who can supersede it. The record
  reads as authority but resolves to no one.
- **Decision text edited in place.** The next reader sees text nobody ever
  agreed to, and the people who relied on the old wording have no way to tell
  what changed. This is the failure that makes a whole `decisions/` folder
  untrustworthy, because it is invisible.
- **A one-way supersession.** The new record says it supersedes the old one, but
  the old one still reads as accepted. Whoever arrives from a search, a
  citation, or an old pull request follows a decision that was replaced.
- **`stage: verified` set on a green build.** Compilation proves it builds, not
  that the decision was carried out. The stage then lies about the only thing it
  exists to say.
- **A ruling that stayed in chat.** It binds the next change and nobody can find
  it. The symptom is the same argument being had a second time, with a different
  outcome.
- **One record settling three questions.** It cannot be superseded for one of
  them, so it survives past the point where a third of it is wrong.
- **An `applies-to` that matches nothing.** Step 1 returns zero records and reads
  as "nothing binds this change" instead of "the glob is wrong". Report the count
  so an empty result is visible rather than reassuring.

## How it is proven

- **Consult ran and found something specific.** The count from step 1 appears in
  the report, and the ids it returned appear on the work item, in the pull
  request body, and in a `Decision:` commit trailer.
- **Acceptance resolves.** The record carries a `decided` date and a `decider`
  that names a person you could actually ask.
- **Supersession is traversable.** Follow `superseded-by` forward and
  `supersedes` back; both land on the other record. Search the repository for
  the superseded id and confirm no live citation still points only at it.
- **The stage matches the evidence.** `implemented` is confirmed by the change
  being in the tree; `verified` is confirmed by naming the signal — the command,
  the gate, the observed behavior — that was watched *this time*.
- **The index resolves.** Every record in `decisions/` appears in the index, and
  every index entry resolves to a file.
