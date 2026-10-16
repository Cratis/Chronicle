<!-- cratis-ai-managed: skills/cratis-screenplay-scenario-coverage/references/version-skew.md -->
# Version skew: reaction cascades and false PLAY0285

How to specify reaction cascades across compiler versions.

Under ESM v6 a command spec's `then` lists the events reactions cascade from it. A compiler
bundled in an older `cratis` reports a **false PLAY0285** for such
a spec; the standalone `screenplay` and a `cratis` whose compiler admits v6 accept it.
Neither checks that a cascade event is *missing*.
- A compiler that admits v6 (standalone, or a current `cratis`): write the v6-correct spec
  (own productions plus cascade); validate with `screenplay <folder> --warnaserror --no-color`
  or `cratis screenplay validate <folder> --warnings-as-errors`.
- Only an older `cratis` bundle and no standalone tool: keep the command spec to its own productions, specify the reaction in its
  Automation slice (`when append <Trigger>` -> `then <Produced>`), record the pending cascade
  expectation in `STATE.md` as known-incomplete. Never delete the reaction.
