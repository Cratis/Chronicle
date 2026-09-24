---
name: cratis-release-notes
description: Draft or review developer-facing release notes and migration guidance from verified changes for a specific Cratis product version. Use for GitHub releases, upgrade guides, and release announcements; not for deciding release authority, publishing, or selecting a version label.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-release-notes/SKILL.md -->

# Release notes for the person upgrading

An upgrader wants to know whether their application changes, what will break,
and what to do before installing. A feature list and a list of pull request
titles cannot answer that. This skill writes the *communication*, not the
release decision. The repository's release workflow owns version intent,
gates, publication and recovery; drafting never authorizes publishing or
editing a release. If the task also includes release planning, select the
separate `cratis/methodology/governed-releases` profile as needed; that is not
required to draft notes.

## Establish the exact subject

Before writing, identify product, tag or candidate version, previous version,
artifact/package names, supported platforms, and the audience. Inventory
observable changes from the diff, specs, linked issues/PRs, and authoritative
product source at that revision. A PR description may be published verbatim as
release notes in a Cratis repository; follow its current template and confirm
that behavior before drafting. Do not turn an unreleased draft into a claim
about shipped behavior.

For each change, ask: who uses this path, what happened before, what happens
now, is action required, and how would they notice? Check changed defaults,
serialization/schema/data migrations, runtime and dependency compatibility,
deploy ordering, deprecations/removals, and upgrade/rollback limits. Report
'not verified' rather than guessing about a supported configuration.

Cratis products move together at their seams: the Chronicle kernel and its
clients, Arc and Chronicle, Components and Arc. When a change touches a
seam, state the pairings you verified (for example, which kernel versions a
client release was tested against) and say which pairings are unverified.
There is no central compatibility matrix to defer to, so the release note may
be the only place a reader learns it. Where a measurement drove the change,
such as how many deployments hit a failure, give the number; never estimate
one to add weight.

## Write for the right channel

- **Exact-version release note (GitHub/PR):** start with changes requiring
  action and affected workflows, then new capabilities and fixes. State impact
  and the user's next action in plain language. Include an issue reference only
  when verified, and follow the owning repository's closing-keyword policy.
  Do not list internal refactors or specs that change nothing users observe.
  For a user-visible fix, a sentence of root cause and of what now guards
  against a regression, stated as observable behavior rather than a list of
  specs, is user-facing: it tells the reader whether to trust the fix. Credit an external contributor by name or handle and say what
  they did, unless they asked not to be named.
- **Migration guide (durable product docs):** a compact *old behavior → new
  behavior → required action* table for each affected upgrade path, followed
  by source-verified before/after code or commands. Distinguish required
  migration from optional cleanup, name sequencing for schema/data changes,
  and explain escape hatches with their cost or expiry. If no action is needed,
  state why and for whom, rather than implying it for everyone. State the
  table's scope and its known exclusions, so a reader who does not find their
  API knows whether it was assessed. Do not infer backward compatibility for
  APIs or configurations that were not assessed.
- **Announcement or blog post:** explain the motivation and show a small
  realistic success, then link to the exact-version note and migration guide.
  It is not a second independent changelog. Do not claim personal experience
  on behalf of a named author without their review.

Keep the release note concise enough to scan, but do not compress away the
boundary that makes a change safe to adopt. One item can follow this shape:

> Applications that run **[workflow]** on **[affected versions]** may observe
> **[symptom]**. In **[new version]**, **[new behavior]**. **[Action]** before
> upgrading; **[unaffected path]** does not need to change. [Migration guide].

That is a checklist for facts, not a template to repeat word-for-word across
items. Use descriptive headings, meaningful links, and natural sentence rhythm.

## Verify before handing over

1. Reconcile every version, affected range, capability and API against the
   release tag or exact candidate and published artifact when available; verify
   each reference and link. A blog post or release summary is a pointer to
   source, not proof that a compatibility issue is resolved.
2. Have a reader of the affected integration check that the instructions are
   actionable. Exercise upgrade commands and compile before/after examples
   with stated versions when feasible; use **cratis-technical-examples** for
   the example workflow.
3. Recheck the evergreen migration guide after release. Remove 'upcoming',
   prerelease pins and obsolete workaround language only after verifying the
   final tag. Separate live guidance from a historically accurate note for an
   older release. Preserve the owning site's machine-consumed guide format:
   Cratis's upgrade picker reads `upgrading/major-versions.md` headings such as
   `### 18 to 19`, linked at-a-glance rows, `**Title** — released YYYY-MM-DD`
   metadata, and explicit `**You do:**` actions.
   Check `sync-upgrade-paths.mjs` and an existing guide before changing that
   structure, then verify the generated picker as well as the page.
4. Name the checks actually run and the combinations not checked. Do not
   imply that a green docs build proved an upgrade safe.

The pattern is informed by [Wolverine's migration guide](https://wolverinefx.io/guide/migration.html)
and [Marten's migration guide](https://martendb.io/migration-guide.html),
which separate concrete upgrade actions from version-specific release notes.
Their current wording is not authority for Cratis versions or APIs.
