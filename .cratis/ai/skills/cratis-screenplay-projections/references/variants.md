<!-- cratis-ai-managed: skills/cratis-screenplay-projections/references/variants.md -->
# Variants

This reference holds the variant excerpt, the mutual-exclusion rule through the event source identity and variant diagnostics.

## Variants — one identity, mutually exclusive read models

Excerpt: the events and the three read models (each with a keyed query) are
declared elsewhere. Every event about an issue is appended with the issue's
identifier as its event source.

```screenplay excerpt
projection WorkItem
  from TitleChanged
    title = title
  variant BacklogItem
    enters on IssueCreated
  variant DevelopmentItem
    enters on IssueStarted
  variant PullRequestItem
    enters on PullRequestCreated
```

Each variant is its own read model; only its `enters on` events create it.
Everything else, including a projection-level shared handler, is update-only: it
becomes a join on the variant's identifier, matched against the handler's key
(here the event source).

⚠️ **Mutual exclusion works through the event source identity.** Entering a
variant removes the entity from its siblings, and that removal is always keyed by
the entering event's source identity, whatever key the `enters on` line declares.
Keep the event source equal to the entity's identifier, as above. An
`enters on IssueStarted key issueId` whose `issueId` differs from the event
source creates the development item under `issueId` but removes the backlog item
keyed by the event source instead, so the issue stays in both variants.

Diagnostics: no `enters on` (`PLAY0382`), a shared mapping a variant's read
model lacks (`PLAY0383`), duplicate variant names (`PLAY0384`), an entering
event claimed twice (`PLAY0385`). The executable model binds variants the way Chronicle's client SDK
reclassifies them; Chronicle's hosted declaration language does not lower them
yet. Use variants for genuinely different shapes, not for
a single `status` field.
