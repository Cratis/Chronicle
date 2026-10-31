<!-- cratis-ai-managed: skills/cratis-screenplay-modeling-lifecycle/references/intake.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Interview phase (P0)

**Skip if** the request already states the domain, the source, the goal and the constraints.
Otherwise ask only what is missing:
1. **Domain**: what business process, in two or three sentences?
2. **Source**: requirements, ideas, an existing system (legacy entry), a `.play` (change entry)?
3. **Goal**: learning, documentation, validation, executable behavior, generated code (the mode)?
4. **Constraints**: outside systems, data sensitivity, who can answer, target stack.
5. **Starting point**: from scratch, or which phase outputs exist?

Confirm in one sentence only when something was inferred: "So we model <domain>, goal <goal>,
mode <mode>, starting at <phase>. Right?" Unattended: assume visibly and record asked versus
assumed in STATE.md (`stop-or-assume.md`). Mid-workflow entry: ask which phases are
done; never rerun a completed one, start at the first incomplete.

Open business questions become anchored `question` notes, with their note IDs in
STATE.md. Read [durable questions](stop-or-assume.md#durable-question-notes) when
recording an assumption or awaiting an answer.
