---
name: cratis-technical-examples
description: Use when adding or reviewing runnable Cratis examples, tutorial projects, multi-client snippets or command/output pairs. Not for page placement, release notes or treating rendering as compilation proof.
license: MIT
metadata:
  cratis-hint-paths: "**/Samples/**/*.{cs,ts,tsx}"
---
<!-- cratis-ai-managed: skills/cratis-technical-examples/SKILL.md -->

# Technical examples readers can trust

A reader will paste the example before they read its explanation. Make that
first attempt work, and show how to tell. A substantial tutorial should have a
real project behind it; a five-line illustration need not become a new sample
repository. This skill owns the example workflow, not the owning product's
language-tab layout or documentation site implementation.

## Explain the evidence and the handoff

Make the scope and assumed setup clear for code, commands and configuration;
shared setup can be established once for a sequence. During verification, ask
which component owns the effect, which host runs it, how identifiers connect
steps, and what result, defaults, limitations or recovery the source establishes.
These are private verification questions, not an inventory to publish after
every example. Explain a missing or consequential connection where the reader
needs it, keeping important warnings before the affected action and limits
beside the behavior they qualify. A plausible block followed by a product-name
list is not a causal explanation.
Preserve real package IDs, API names and wire types rather than translating
them into umbrella branding. Follow the
[documentation rule](../../rules/documentation.md) for naming and claims.

Keep source checking, compilation, runtime observation and deployed behavior
separate in the private change note. A source-verified command is not a command
you ran; expected output is not observed output. For a new or materially changed
journey, use **cratis-documentation-writing**'s
[fresh-context reader review](../cratis-documentation-writing/references/reader-review.md).
That comprehension check cannot replace any example gate, and an unavailable
reader review must not be claimed as completed.

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
  Use the same domain type names in every tab; if the prose has to explain
  which name each tab uses, fix the snippets instead. State each backend's id
  and command-response wire type (for example `Guid` versus `string`) and keep
  one id type per concept across every page of a series. Shared frontend code
  that depends on those types must be backend-neutral or tabbed per backend.
- **Sample application:** one realistic domain, explicit prerequisites and
  relevant version constraints, a documented run command, a reproducible state to start
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

## Version constraints belong in the page; the test log does not

Follow the [documentation rule](../../rules/documentation.md): a reader needs
versions that change what they install or write. State minimum, fixed or
breaking releases and compatibility or peer ranges that affect the task.
Ordinary installation follows the supported latest-release path, not universal
exact-patch pinning; check compatibility rather than assuming every newest
package works together. Maintained manifests/lockfiles can carry resolved
versions without repeating them in prose. Pin a deliberately reproducible
tutorial when needed, and preserve required security/deployment constraints
and production image policy.

Keep the author's verification record out of the page. Exact SDK builds, image
digests or commit SHAs recorded for provenance, the date it was run, "tested
with X on Y", "also checked against a development server", counts of passing
specifications and a list of which tabs were not run are all a log of what
the author did. A reader cannot act on them, they are stale by the next
release, and a page that ends in an audit trail reads as generated, not
written. Put them in the change summary, the pull request comment or
`.ai-work/`, per the step above.

Test each version sentence: would a reader install or write something
different because of it? If not, remove it. A real limitation stays, stated as
a fact about the product ("the Kotlin client has no equivalent yet"), not as a
record of what was and was not run. Keep a digest in a runnable command when
reproducibility, security or deployment requires that artifact; its presence
inside a command alone is not a reason to retain it. Removing provenance is
not permission to float a production image or weaken a compatibility boundary.

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
coverage from a site build or a validator that never scans the page. In the
private change note, map changed ordinary Markdown/MDX fences, commands,
configuration and output to the source revision and validator or sample that
actually covers each block. Mark uncovered blocks and unrun checks explicitly;
source-equal, compiled and runtime-demonstrated are different results.

A compiling tab is not a working tab. For each backend, confirm that the
artifact is discovered and registered, that proxy generation emits the file
and types the page names, and that any runtime behavior the page claims (live
updates, constraint rejection) was observed against a real kernel and sink. If
a tab's code compiles but fails at runtime, don't publish it with a workaround
in the prose after it. Put working code in the tab and reference the tracking
issue in a code comment.

A simple process beats a large unmaintained examples gallery: give the reader
one small success first, then link to a fuller sample when they need it. Update
examples alongside public API changes and incoming reports of copy/paste failure.

## Stop conditions

Do not claim a sample is runnable when a dependency, runtime, credential,
external service, supported client, or version needed to reproduce it is
unknown. Do not silently replace a source-verified example with plausible
looking pseudo-code. Identify the missing check or narrow the claim instead.
