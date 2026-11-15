<!-- cratis-ai-managed: skills/cratis-screenplay-scenario-coverage/references/version-skew.md -->
# Specifying reaction chains across slices and compiler versions

How to specify a chain of reactions, and what `PLAY0285` means on each compiler.

Requires Screenplay 4.246.0 or later (`screenplay --version`) for single-slice runs and `then invokes`.

A specification runs only the slice that declares it (Screenplay decision 0115, for every model
and version). A chain across slices is proved link by link:
- The command's slice: `when <Command>` states the command's own events.
- The Automation slice: `when append <Trigger>` states what its reaction appends, and
  `then invokes <Command>` (ESM v34) states a command it invokes in another slice.
- The invoked command's slice: `when <Command>` states that command's events.

A command spec that states another slice's event is refused with `PLAY0285` (unreachable)
and fails at run time with "Expected N fact(s), got M". That is a modeling error: move
the expectation to the slice that produces it. Never delete the reaction.

Older compilers:
- A compiler older than ESM v6 does not trace reactions and can report `PLAY0285` on a
  reaction in the specification's own slice. That is tool skew: validate with the
  standalone `screenplay <folder> --warnaserror --no-color` or a current
  `cratis screenplay validate <folder> --warnings-as-errors`, and record which tool gave which verdict.
- A compiler that predates `then invokes` refuses it. With only such a tool, keep the
  automation's own appended facts in its spec and record the pending hand-over expectation
  in `STATE.md` as known-incomplete.
