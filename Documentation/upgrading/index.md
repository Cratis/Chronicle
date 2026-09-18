---
title: Upgrading Chronicle
description: What a Chronicle major version means for you, and where to find the guide for each one.
---

Every major version of Chronicle has an upgrade guide, and every guide answers the same
question first: **does anything change for you?**

Often the answer is no. Chronicle publishes the kernel, the .NET client, the contracts
package and the language clients from one repository at one version, so a breaking change
anywhere raises the major everywhere. A major can therefore be driven by something you
never call, on a surface you never touch — and upgrading is then a version bump and a
rebuild.

That is worth saying out loud rather than leaving you to work it out from release notes.
A major version that silently changes nothing is far more common here than one that
rewrites your code, and not knowing which you are facing is its own cost.

:::note[A major is permission, not a promise]
Raising the major grants the project permission to break something. It does not promise
that anything was broken, and it does not mean every surface changed. Read the guide for
the version you are moving to before planning work.
:::

## Guides

Start with [Every major version](major-versions.md). It has one entry per major boundary —
what broke and what you do — so you can read only the rows between the version you are on and
the one you are going to, including when that spans several majors at once.

| From → to | What it means for you |
|---|---|
| [Every major version](major-versions.md) | The whole history, 6 → 19, including which two releases to step over rather than onto. |
| [18 → 19](18-to-19.mdx) | Nothing, for almost everyone. One .NET method signature changed; rebuild and move on. |

## How to read a Chronicle version

Chronicle follows semantic versioning, with the major raised for a breaking change to any
published surface:

- **The wire contract** — the gRPC messages and services every language client speaks.
  These changes affect you whichever client you use, and the upgrade guide will say so
  plainly.
- **A published API** — for example a public .NET method signature. These affect only
  the clients that expose that surface, and often only at compile time.
- **Observable behavior** — something that worked one way and now works another.

The label on the pull request that cut the release records which of these applied. When a
major was raised for a compile-time break on one client, users of the other clients are
unaffected beyond taking the new package.

## Upgrading across several majors

Take them one at a time and read each guide. Chronicle's wire compatibility is verified
within a major: every released minor of the major you are on is checked to still be
served by the current build. Crossing a major boundary is where a protocol change is
permitted to have happened, so it is where an upgrade deserves attention.

## Events you already stored

None of this concerns your stored events. Evolving an event's own schema is a separate,
deliberate mechanism with its own generation and migration model — see
[Migrations](../migrations/index.md). Upgrading Chronicle does not rewrite, re-shape or
re-interpret events you have already appended.
