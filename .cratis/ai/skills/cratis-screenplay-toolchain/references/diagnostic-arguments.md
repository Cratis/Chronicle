<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/diagnostic-arguments.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Read diagnostic subjects from arguments

Requires Screenplay 4.194.0 or later (`screenplay --version`) for typed diagnostic arguments; older diagnostics may omit the field.

Use code plus declared `arguments` to identify subjects, never parse message text.
Messages are display prose and may be reworded. Published names, kinds, meanings
and optionality are permanent; tolerate unknown names and keyword values.
Missing `arguments` remains readable; no subjects is `{}`; optional subjects are
omitted when unavailable. This changes neither text output nor ESM version.

The `screenplay` MCP server's `diagnostics`, `read-workspace` executable diagnostics,
proposal `diagnostics` and `introducedExecutableErrors[].arguments`, plus the CLI's
JSON output, expose the same structure. Resolve the harness-native tool name from
the listing; this is not a new diagnostic tool. C# exposes Diagnostic.Arguments.

```json
{
  "specification": { "kind": "specification", "path": ["Billing", "Invoices", "SendReminder", "Overdue"] },
  "step": { "kind": "keyword", "value": "when" },
  "stepIndex": { "kind": "index", "value": 0 },
  "property": { "kind": "text", "value": "amount" }
}
```

Each argument is keyed by name. `text` is verbatim authored text, `keyword` a stable
never-localized language spelling, `index` a zero-based number. A declaration uses
its declaration kind as `kind` (for example `specification`) and `path`, not a
`value`: module if present, features outermost first, slice if present, declaration
name; never repeat a slice's own name. Specification names are authored names;
a separate `case` names an expansion. Argument order is immaterial.

## Codes with declared subjects

`?` denotes optional. Except where marked, arguments are text.

| Code | Arguments |
| --- | --- |
| PLAY0044, PLAY0075, PLAY0101, PLAY0518 | `text` |
| PLAY0351 | `readmodel`, `property` |
| PLAY0468 | `query?`, `argument` |
| PLAY0514, PLAY0519 | `property` |
| PLAY0520 | `example`, `type?`, `kind?` (keyword: event, command, readmodel, concept, type, example) |
| PLAY0521 | `example` |
| PLAY0522 | `example`, `kind`, `expectedKind` (both keyword: event, command, readmodel) |
| PLAY0523 | `example`, `type`, `property?` |
| PLAY0524 | `specification` (declaration), `step` (keyword), `stepIndex` (index), `property`, `type`, `example?`, `case?`, `parameter?` |
| PLAY0525, PLAY0526 | `example` |
| PLAY0583 | `case?`, `parameter?`, `type?`, `text?` |
| PLAY0781 | `command` (declaration), `read`, `readModel` (declaration), `reason` (keyword) |
| PLAY0782 | `command` (declaration), `read` |
| PLAY0783 | `command` (declaration) |
| PLAY0784, PLAY0785 | `command` (declaration), `read`, `path` |
| PLAY0790, PLAY0794, PLAY0799 | `profile`, `template`, `blueprint?` |
| PLAY0791 | `profile`, `template` (the layout name) |
| PLAY0792 | `profile`, `template`, `blueprint` |
| PLAY0793 | `profile`, `template`, `blueprint`, `version` |
| PLAY0795 | `profile`, `template`, `blueprint?`, `outlet` |
| PLAY0796 | `profile`, `template`, `blueprint?`, `option` |
| PLAY0797 | `profile`, `value` |
| PLAY0800 | `subject` (required declaration: command) |

PLAY0781–PLAY0785 need Screenplay 4.207.0; earlier compilers omit these arguments. `read` is the alias when the
command gives one, else the read model name. The PLAY0781 `reason` keywords are listed in command-surface
[protected reads](../../cratis-screenplay-command-surface/references/protected-reads.md); each refused read gets its own PLAY0781.

Requires Screenplay 4.210.0 (`screenplay --version`) for PLAY0800 (`CommandHandlerRequiresAttachment`) and its `subject`; older binders report attached command handlers as PLAY0268. `arguments.subject.kind` is `command` and `path` names the command. Treat the refusal as permanent: deliver a constrained implementation attachment by code gap-fill, not by waiting for ESM admission. Switch on the code and navigate the subject, never match message text.

For PLAY0524, `step` is given, given readmodel, when, when append or then.
`stepIndex` counts every authored given/when/then clause once across all forms,
not nested lines, synthesized callers or defaults. Example/case expansion retains
that position. Read the missing property and its type directly, navigate the
specification path and optional case, then repair the effective fixture; do not
infer these from a translated message. Codes outside this table may have no
subjects, including PLAY0198. Check the published contract for the code rather
than assuming every diagnostic names a declaration.
