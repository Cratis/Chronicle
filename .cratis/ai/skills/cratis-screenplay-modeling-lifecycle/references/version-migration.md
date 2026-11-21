<!-- cratis-ai-managed: skills/cratis-screenplay-modeling-lifecycle/references/version-migration.md -->
# Migrate a model before editing it

Requires Screenplay 6.0 or later (`screenplay --version`) for `screenplay migrate` and the MCP `migrate` tool.

A Screenplay major version retires forms without a deprecation period: the old form is an error whose message names the
replacement and the migration that writes it, for example
`run 'screenplay migrate --from 5 --to 6' to rewrite the model (slots and outlets)`. Editing such a model by hand mixes
two changes in one diff and loses what the migration reports for review, so migrate first, as its own change.

## When

- The compiler reports a retired-form code (`PLAY0890`-`PLAY0896`), `PLAY0026` on a template `toolbar` slot that holds
  buttons, or any message naming `screenplay migrate`.
- The model still writes a Screenplay 5 form: a region declared as `outlet <name>` with no kind, `destination outlet`,
  `navigation topbar|sidebar`, `<slot> contributes <Point>`, or a `toolbar <name>` on a feature or module.
- The project records that its model targets Screenplay 5 and the installed compiler is 6.

A file does not say which version it was written for, so the author or the project decides `--from`.

## How

1. Start from a clean working tree under the model root, so the migration is the only diff.
2. Preview: `screenplay migrate <model-root> --from 5 --to 6 --dry-run --report .ai-work/<task>/migrate.json`.
3. Run: `screenplay migrate <model-root> --from 5 --to 6`. A folder migrates every `.play` file beneath it as one
   application; never migrate one file of a split model on its own unless the brief names it.
4. Read the exit code:

   | Exit | Meaning | Next |
   | --- | --- | --- |
   | `0` | Every file migrated and compiles warning-free with Screenplay 6 | Review the diff, then compile |
   | `2` | A file needs review, or the result still has compiler errors | Decide each review item (below), then compile |
   | `1` | The migration could not run (bad argument, no files, no step chain, a file a step could not rewrite) | Fix the cause; the files that could not be rewritten are unchanged |

5. Decide every review item the report lists, with its file and line. Common ones: a template slot that holds items and
   something else (place the rest with a screen's `fits`, or remove it); a toolbar `item <name> action|navigate|dialog`
   in a feature toolbar (write a `button ... slice <Slice>` hook or remove it); a `logo` below the layout (only the layout
   declares it).
6. `screenplay <model-root> --warnaserror` is clean, and `screenplay test <model-root>` passes as before.
7. Record the migration as its own change (commit, or a separate proposal through MCP), then start the requested edit.

Through MCP, the `screenplay` server's `migrate` tool takes `from`, an optional `to` and an optional root-relative
`document`. It is read-only: it returns the report and, for a named document, the migrated source to write through the
usual file tools or a proposal. The VS Code command **Screenplay: Migrate document** migrates the open document.

## Never

- Never hand-translate the old forms instead of running the migration: it prints only the lines it changes and keeps
  comments, alignment and every other line byte for byte.
- Never accept a review item silently or leave the model with the errors the migration reported.
- Never edit Stage-managed output to match; render again from the migrated model.
