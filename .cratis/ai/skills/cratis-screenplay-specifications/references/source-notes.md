<!-- cratis-ai-managed: skills/cratis-screenplay-specifications/references/source-notes.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Retained source notes

The original worked example had reference-runner evidence in Screenplay
`Documentation/screenplay/{specifications,policies,constraints,readmodels,diagnostics}.md`;
inline events and canonical optionality added later need their own current check.

Actions beyond commands (`given clock`, `when clock`, `when trigger`, `given capture`,
`when capture`, `when query`, `then result`, `then no result`) follow
`Documentation/screenplay/{specifications,diagnostics}.md`.

| Area | Purpose |
| --- | --- |
| Parser, binder and reference execution | Original evidence for the worked example |
| Inline facts, `optional` and clock spelling | Changed examples compile; the reference runner was not rerun for them |
| ESM v7 | Generated fixtures and `then returns` (`specifications.md` "Generated fixtures and return expectations"): the excerpt matches the compiled example in `cratis-screenplay-toolchain`, whose specifications run in the reference runner |
| ESM v6 | Specification actions and reaction cascades, `then no readmodel`, `$strings` rejections, `invokes` caller rule |

The inline-facts update followed `commands.md`, `events.md`, `types.md` and
`specifications.md`. That pass compiled the changed examples; it did not rerun the reference
runner. Keep this source evidence distinct from the current source-check gate and V4 execution.
