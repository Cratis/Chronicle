<!-- cratis-ai-managed: skills/cratis-documentation-writing/references/reader-review.md -->
# Fresh-context reader review

Use this procedure for a new or materially changed documentation journey. Keep
it proportional: a short how-to needs a task check, not a simulated onboarding
program. A reader may be a person or, when authorized, a separate fresh-context
agent. Do not launch another agent or model evaluation without authorization.
If independent review is unavailable, report it as unavailable; an author's
self-review is not a substitute.

## Give the reader only what the page gives them

Identify the intended audience and one concrete task. Supply the page or bounded
export and only its explicitly stated prerequisites and links, not the author's
answers, private source notes, implementation context or a coached sequence.
Record what the reader actually received. The reader follows that material and
reports what to do, what happens and how to recognize success. Do not silently
repair missing setup from personal knowledge. Guesses and unprovided
prerequisites are documentation gaps even if the reader eventually succeeds.

Use the same reader questions for a Platform front door, a single-component
procedure or a cross-component journey; do not force the whole Platform into
a task that needs only one component:

- Can you identify the first action and its prerequisites without guessing?
- What changed after the step, and what visible result proves it?
- Which component owns each responsibility or handoff? When code and
  configuration use different host processes, where does each piece run?
- Which names, identifiers, file paths and wire types connect the steps?
- What are the defaults, limitations and a likely failure or recovery path?
- Does a linked next step continue the task rather than leave a dead end?

Observe the smallest gap that blocks or misleads this reader. Repair the
prerequisite, handoff, identifier or explanation at that point, then recheck
the affected passage without leaking the answer into the review. Do not
redesign navigation or rewrite the whole corpus as a side effect. Hand wider
changes back to the owner.

## Select the perspectives that can expose this page's gaps

Choose lenses relevant to the task, not a mandatory review board or a fixed
number of agents. One reviewer may cover several; a short change may need only
one task check. Keep the independent task attempt page-only and uncoached.
Source/security reviewers may inspect authoritative evidence separately; do
not feed their answers into that attempt.

| Lens | Question when relevant |
| --- | --- |
| Newcomer | Can someone start, follow the causal steps and recognize success without private setup knowledge? |
| Experienced developer | Can someone find the contract, adapt the example and identify boundaries without replaying a whole tutorial? |
| Operator | Are host/configuration ownership, safe defaults and source-backed recovery links clear? |
| AI/text reader | Does the bounded export preserve the task, warnings, variants, links and canonical citation context? |
| Security/trust boundary | Are credentials, network exposure, permissions, destructive actions, tenant boundaries and preview/production limits accurate and visible before the risky step? |
| Accessibility/visual/editorial | Do semantic order, keyboard/focus behavior, text alternatives, small screens, contrast and wording help the task where applicable? |

Check accuracy against source independently of style. Cards or tabs should
clarify a choice; a diagram needs meaningful text, not just an image. An export
or image label alone proves neither accessibility nor AI comprehension. Link
only recovery supported by the owning source, not invented commands or errors.
Record the demonstrated gap, repair it and recheck the affected passage once;
broaden review only for new evidence, not repeated polishing loops.

## Keep evidence levels separate

Record each level independently; one passing level cannot stand in for another.

| Level | What it establishes | What it does not establish |
| --- | --- | --- |
| Comprehension | A fresh reader can recover the task, boundaries and expected result from the supplied material | That any API exists or code runs |
| Source checking | Claims, signatures and commands match authoritative source at the stated revision | Compilation or behavior |
| Compilation | The actual displayed example compiles at its stated scope | Runtime behavior, registration or discovery |
| Runtime | The example produces the claimed behavior in the named local environment | Deployment or another host, backend or version |
| Deployed | The deployed page, route or service was checked in its actual environment | Untested variants or general assistant answer quality |

A site build checks rendering; a static routing check checks the corpus's
structure. Neither proves comprehension. Reading expected output is not
observing runtime output. For commands and snippets, use
**cratis-technical-examples**; for bounded exports and source citations, use
**cratis-llm-friendly-documentation**.

## Report only what happened

Keep a concise private author/change note: audience and task; supplied material;
reader and context isolation; guessed prerequisites and failure points;
smallest repairs; checks run and remaining gaps. For AI-assisted work, include
actual skill paths, source revision, and observed read order when available.
Do not infer reads from an installed skill, a route or an invocation, and do
not fabricate unavailable transcript evidence. This is not a public verification
receipt, a new tracking system or a reason to add test logs to the page.
