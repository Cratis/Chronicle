<!-- cratis-ai-managed: skills/cratis-screenplay-specifications/references/source-notes.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Retained source notes

Original evidence: Screenplay v4.31.0 (`355dffb`),
`Documentation/screenplay/{specifications,policies,constraints,readmodels,diagnostics}.md`.
The original worked example had reference-runner evidence at that tag; inline
events and canonical optionality added later need their own current check.

Actions beyond commands (`given clock`, `when clock`, `when trigger`, `given capture`,
`when capture`, `when query`, `then result`, `then no result`) were checked at
v4.48.0 (`3baf4a4`), `Documentation/screenplay/{specifications,diagnostics}.md`
and proposed decision 0022. These actions do not exist before v4.48.0.

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Screenplay` | `4.31.0` | Original parser, binder and reference execution evidence |
| `Cratis.Screenplay` | main `fd18129` | Inline facts, `optional` and clock spelling; changed examples compiled |
| `Cratis.Screenplay` | `4.68.0` (`79801bf`) | ESM v7 generated fixtures and `then returns` (decision 0026, `specifications.md` "Generated fixtures and return expectations"): the excerpt matches the compiled example in `cratis-screenplay-toolchain`, whose specifications run in the reference runner |
| `Cratis.Screenplay` | `4.66.0` (`c89198b`) | ESM v6 specification actions and reaction cascades, `then no readmodel`, `$strings` rejections, `invokes` caller rule; probed with the standalone tool and its MCP server |

The inline-facts update followed `commands.md`, `events.md`, `types.md` and
`specifications.md` at main `fd18129` (after v4.52.0). That pass compiled the
changed examples; it did not rerun the reference runner. Keep this historical
source evidence distinct from the current source-check gate and V4 execution.
