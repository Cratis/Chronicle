<!-- cratis-ai-managed: skills/cratis-engineering-decision-record/references/record-format.md -->
# Decision record format

The shape below is the one Cratis repositories that keep a `decisions/` folder
converge on. A repository that already defines a stricter local shape stays
authoritative; add fields there rather than dropping the ones listed here.

## Front matter

| Field | Required | Meaning |
| --- | --- | --- |
| `id` | Yes | Stable identifier, unique in the repository, never reused after supersession. |
| `title` | Yes | The single question the record settles, stated as a choice. |
| `status` | Yes | Where the record stands in its own review lifecycle. Closed set below. |
| `stage` | Yes | How far an accepted decision has travelled from words into observed behavior. Closed set below. |
| `class` | Yes | What kind of choice this is, which sets who may settle it. Closed set below. |
| `reversibility` | Yes | What undoing it would cost. Closed set below. |
| `decided` | On acceptance | The date the decision was accepted. |
| `decider` | On acceptance | A named person. Never a role, a team, or a tool. |
| `applies-to` | Yes | The paths this record binds, as globs. What the consult step matches against. |
| `supersedes` | When replacing | The id of the record this one replaces. |
| `superseded-by` | When replaced | The id of the record that replaced this one. |

`status` and `superseded-by` move together: a record marked `superseded` without
a forward pointer strands every reader who arrives at it.

## Closed value sets

Do not invent a word for a state one of these sets already names.

**`status`**

| Value | Meaning |
| --- | --- |
| `proposed` | Written and offered for a verdict; not yet in force. |
| `returned` | Sent back to the proposer for revision; not a rejection. |
| `accepted` | In force; binding on work that touches the paths it covers. |
| `rejected` | Refused; the choice it proposed is not taken. |
| `deferred` | Deliberately not settled yet, with the reason recorded. |
| `superseded` | Replaced by a later record, which it points at. |

**`stage`**

| Value | Meaning |
| --- | --- |
| `none` | Accepted, but nothing has been built against it yet. |
| `implemented` | The change the decision calls for exists in the tree. |
| `verified` | A signal observed this time confirms the implementation. |

**`class`**

| Value | Meaning |
| --- | --- |
| `strategy` | Direction, portfolio, or ownership of a body of work. |
| `contract` | An interface, schema, protocol, or release boundary others build on. |
| `product` | What is built, for whom, and what it promises. |
| `working` | A local, reversible choice inside one team's own scope. |

**`reversibility`**

| Value | Meaning |
| --- | --- |
| `reversible` | Undone at negligible cost; decide fast and revisit. |
| `costly` | Undone, but only by paying real migration or rework cost. |
| `irreversible` | Cannot be undone; requires a human verdict before acting. |

`class` and `reversibility` together say who may settle the record. A `strategy`
or `irreversible` record is never accepted by an agent.

## Body sections

A record's body carries, in this order:

1. **Context** — the situation that forced a choice, and what changes if nobody
   chooses.
2. **Decision** — the choice, in one paragraph, in the present tense. This is the
   text that is never edited in place once the record is accepted.
3. **Options considered** — including the one not taken and why. This is the part
   a future reader needs most and the part nobody remembers.
4. **Default if unanswered** — what happens if the question is never settled, and
   what that costs. A record without this cannot be weighed against doing nothing.
5. **Timeline and scope** — the horizon the decision holds to, what is in scope,
   and what is explicitly out.
6. **Verification** — `Done when` and `Verify by`, written before acceptance. The
   observable signal that says the decision was carried out.
7. **Consequences** — what this makes easier, what it makes harder, and what it
   forecloses.

## Corrections after acceptance

A typo fix or added context goes under a dated banner inside the record:

```markdown
> **2026-03-04 — clarification.** The decision text below said "client"; every
> use of that word means the generated client SDK, not a consuming application.
> The choice itself is unchanged.
```

Anything that changes the choice is a new record with two-way pointers, not a
banner.

## Index

The folder carries an index listing every record with its id, title, status,
stage, decided date, and decider. The index is regenerated whenever a record is
added or its status changes; a record the index omits is a record the consult
step will never find. Two checks keep it honest: every file in the folder appears
in the index, and every index entry resolves to a file.
