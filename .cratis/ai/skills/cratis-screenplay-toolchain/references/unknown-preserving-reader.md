<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/unknown-preserving-reader.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Unknown-preserving semantic model reader

Requires Screenplay 4.256.0 or later (`screenplay --version`). Source: Screenplay
`Documentation/screenplay/interoperability.md` ("Minor versions and unknown-preserving
admission").

A consumer that must retain unknown additions without executing them reads a model with
`UnknownPreservingSemanticModelReader.Read(bytes, EsmAdmissionRange)`. It is an opt-in reader beside
the strict reader, which is unchanged and still refuses every unknown member and unregistered
version. Reading carries no producer trust and no execution, rendering or reverse-recovery
authority; those admissions stay separate decisions.

## Admission

- The caller supplies one major with inclusive minimum and maximum minor. Both wire versions must
  lie in that range, and the package must ship a frozen baseline descriptor for that major. The
  reader is not limited to the latest major.
- Unknowns are admitted only above the major's highest registered minor. An exact registered pair
  admits none.
- Classify admission from the document's own wire pair, not from the pair of the validated known
  projection, which uses the highest registered pair of the major.

## What the reader keeps

- The original bytes, wire versions and full revision; a validated known projection; and
  owner-addressable diagnostics for each unknown.
- Known or unknown is decided from the document's own major against the frozen baseline field set,
  which holds what the published writer emits (for example `predecessor` and `priorRevisions` only
  from major 4). Members after the baseline sit in an ordinal extension region.
- An unknown is security flagged only through a known structural position: authorization, policies,
  callers and claims; identity mapping; compliance, personal and secret data; event-stream routing.
  Every other unknown is Unclassified, not automatically unsafe. Never infer meaning inside an opaque
  unknown subtree; consumers decide degradation and whether to disable an owner.
- Preserved unknowns are re-emitted byte-identically and never executed.

## Refusal boundary

A breach refuses the whole document with `InvalidSemanticContract`; nothing is truncated:

- a malformed or missing wire version, a version outside the range, or a language and semantic pair
  that does not match;
- no frozen baseline descriptor for the major;
- duplicate members, non-NFC text or bytes that are not canonical, and a revision that does not match
  the canonical bytes;
- container depth over 64, input over 8,388,608 bytes, more than 64 unknown boundaries per owner or
  1,024 per document, more than 1,048,576 unknown value bytes, or an unknown name outside
  `[a-z][A-Za-z0-9]*` of at most 64 characters (nested names included);
- an unknown discriminator value, or a known dangling reference.

Inside an opaque extension, object names are ordinal at every depth, arrays keep received order,
and absent stays distinct from null.

Renderer admission of an unknown-bearing model is separate work: this reader proves the bytes
were preserved, not that a Stage renderer accepts them.
