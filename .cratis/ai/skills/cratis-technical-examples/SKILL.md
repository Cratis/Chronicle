---
name: cratis-technical-examples
description: Design and verify developer-facing code samples, tutorial projects, and documentation snippets against real Cratis APIs. Use when adding or reviewing a runnable example, multi-client snippet, sample app, command/output pair, or migration before/after code. Do not invent API shapes or treat rendering as a compilation check.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-technical-examples/SKILL.md -->

# Technical examples readers can trust

A reader will paste the example before they read its explanation. Make that
first attempt work, and show how to tell. A substantial tutorial should have a
real project behind it; a five-line illustration need not become a new sample
repository. This skill owns the example workflow, not the owning product's
language-tab layout or documentation site implementation.

## Choose the right source

- **Short illustration:** use the smallest self-contained block that proves one
  point. Verify every framework type, import, signature, and option against
  first-party source at the supported version. State any domain types or setup
  the excerpt assumes; do not present a partial excerpt as a standalone app.
- **Multi-step tutorial or long example:** write or find a buildable sample or
  spec maintained with the product. Extract the displayed snippet from that
  source when tooling supports it. Otherwise keep a reproducible extraction or
  snippet-equality check beside the sample and run it with the sample build in
  CI. Manual comparison alone is a one-time review, not a drift guard; name
  that limitation when automation is unavailable. Do not copy once and let
  the page drift. Link each extracted block to its source file so a reader
  can follow it to the fuller example; that link is navigation, not a
  verification receipt. The Cratis site adds these links automatically to
  client-language tabs, so don't add them by hand there.
- **Multi-language client example:** keep one explanation; use the established
  client-owned snippet mechanism, compile each client against its own SDK, and
  offer only implementations that exist. Do not translate a C# call by guess.
- **Sample application:** one realistic domain, explicit prerequisites and
  package versions, a documented run command, a reproducible state to start
  from, and a visible result. Keep it minimal enough for a new reader to finish.
  Link from the documentation step to the precise sample file, not just the
  repository root. State how to reset sample state safely, what resources the
  example creates, and any costs or security shortcuts that make it unsuitable
  for production. Never use real credentials or personal data.

For Cratis-specific API traps and source-location guidance, read
[Writing Correct Code Examples](../../rules/writing-correct-examples.md).
A local Chronicle client documentation workflow takes precedence for shared
language tabs. Never hand-edit a generated proxy or a synchronized docs copy.

## Build the example as a reader would

1. Decide what the example demonstrates and what it deliberately omits. Check
   the version the page targets, not an arbitrary sibling checkout at HEAD.
2. Start with a working program, spec, or focused use of the public API. Model
   the product's intended idioms, not just a way that happens to compile. Name
   a common non-idiomatic trap where it helps the reader avoid a likely mistake.
   Check
   packages, imports, namespaces, required attributes, method receivers and
   overloads against product source. Invented *domain* names are fine;
   invented framework APIs are not.
3. Show enough context to paste and run: required setup, the file or project
   location, a language-tagged fence, commands in execution order, and expected
   output or resulting state. Do not hide essential steps behind `// ...`.
4. Run the owning build/spec or snippet extractor and the sample command where
   feasible. Make the expected result an assertion or inspectable output, not
   just 'the command exited 0'. A rendered code fence proves syntax highlighting,
   not compilation or behavior.
5. Check documentation links, accessibility of diagrams/images, and that the
   extracted block still matches the compiled source. For a sample that uses
   external services, name their setup and the check that could not run.
6. Record the source revision and verification scope in the change summary,
   not as a permanent receipt embedded in the page.

## Know what the owning gate actually checks

Look for a snippet validator before designing another checker. Arc and
Chronicle's .NET repositories use `Documentation/validate-client-snippets.py`
for supported **C#** fences in `Documentation/client-snippets/**`; Arc's
validator has a `--self-test` that plants a failing snippet. Other client
repositories own their own validators and toolchains. Check the validator's
reported count and exclusions: unsupported markers or legacy snippets are not
proved compilable just because the run passed. Run the owning validator for
every changed client snippet, or name a missing toolchain and rely on its
required CI gate. These checks **do not** compile ordinary Markdown/MDX fences
elsewhere in product documentation. For those, verify against real source and
a runnable sample/spec or an explicit snippet comparison; do not claim
coverage from a site build or a validator that never scans the page.

A simple process beats a large unmaintained examples gallery: give the reader
one small success first, then link to a fuller sample when they need it. Update
examples alongside public API changes and incoming reports of copy/paste failure.

## Stop conditions

Do not claim a sample is runnable when a dependency, runtime, credential,
external service, supported client, or version needed to reproduce it is
unknown. Do not silently replace a source-verified example with plausible
looking pseudo-code. Identify the missing check or narrow the claim instead.
