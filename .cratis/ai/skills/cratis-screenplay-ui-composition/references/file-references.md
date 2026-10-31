<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/file-references.md -->
# The file directive

This reference holds the three meanings of `file` and the rules for paths and loading.

## `file` — implementation or provenance, never a substitute

Three meanings, one word:

- **Backend implementation attachment.** On an attached command `handler`
  (PLAY0800: permanent constrained-attachment requirement, gap-filled in code;
  pending described handlers instead bind v23 without a file), a validation
  rule predicate, a query `performer`, a reducer rule, a reaction trigger, a
  `constraint` and a `policy`, `file` stands in for the inline body: the
  implementation lives there. These are the files a host inventories.
- **UI realization file.** On a `screen`, `file` names the file that realizes
  the screen. It sits on the **screen itself**, not on a directive inside it:
  `File` is a member of `ScreenSyntax`, and the directive types have no such
  member. Screens are deferred from the executable model (`PLAY0269`), and the
  attachment loader does not collect screen files: they are never loaded, hashed
  or checked, so a missing screen file gets no `PLAY0430`–`PLAY0434` warning.
- **Provenance.** On a **pure declaration** — `concept`, `type`, `event`,
  `readmodel`, `projection`, `slice`, `specification`, top-level `trigger` — it
  only records which file realizes it.

Requires Screenplay 4.210.0 (`screenplay --version`) for the file/inline handler's PLAY0800; older binders use PLAY0268. Read its required typed `arguments.subject` to identify the command, never the message.

The rules:

- **Repository relative, never absolute** (an absolute path is warning `PLAY0264`).
- **Syntax compilation never resolves it.** The `screenplay` tool and
  `PlayFileCompiler` do not read files, so a stale path does not invalidate the
  document. A host that loads attachments (the MCP server, `AttachmentFiles.Load`)
  reads **backend implementation** files only, resolved from the model root
  rather than the `.play` file's directory, to hash their content; a refused or
  missing file stays unresolved with a `PLAY0430`–`PLAY0434` warning. Screen
  files and declaration-only `file` references are never read; check a screen
  file's path yourself.
- **It never replaces the declaration** — a `projection` still declares its
  blocks, an `event` still declares its properties.
- **Loaded is not run.** Screenplay hashes attached code and can map an inline
  body back to its source for an editor, but never compiles or executes it.
