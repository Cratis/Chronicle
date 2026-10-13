<!-- cratis-ai-managed: skills/cratis-screenplay-modeling-lifecycle/references/principles.md -->
# Principles and rejected anti-patterns (binding guidance)

Derived from the Screenplay and Stage roadmap and strategy decisions. Each principle is an
instruction for the work, not background reading. Skills that restate one cite its number.

1. **Change accepted intent in Screenplay first.** Generated code realizes the model; it never competes with it.
2. **Work within an explicit model boundary and change it in slices.** One successful slice is not a complete application.
3. **Report five different verdicts separately** (V1-V5, `verdicts-and-modes.md`).
4. **Discover installed capabilities and versions before prescribing tools or syntax.** A dependency bump does not prove support.
5. **Use typed workspace proposals, exact revisions and explicit review before apply.** A refusal is never permission for a regex or text replacement.
6. **Keep semantic identity independent of names, paths, layout and generated symbols.** Commit the identity catalog with the model.
7. **Keep unsupported intent visible.** Return a precise gap and its owning issue; never weaken the model so rendering passes.
8. **Write specifications with intent, including rejection and denial.** Never report an unexecuted or unsupported scenario as passed.
9. **Treat state reads as a correctness boundary.** Do not claim protection until the opted-in contract and the target support exist.
10. **Separate event-source identity from event payload and choose destinations explicitly.**
11. **Persisted event shape changes are evolution, never cleanup.** Keep historical generations.
12. **Model external work as business intent.** Provider interfaces and transport mechanics stay out of the language.
13. **Use explicit implementation attachments for necessary code.** Keep AI out of deterministic builds and planners; AI realization is requested, verified and accepted separately.
14. **Never conflate persona, caller, authorization subject, compliance subject, event source or tenant.** Hiding in the UI is not server enforcement.
15. **Honor the runtime's semantic authority** without importing its implementation vocabulary (Chronicle defines projection, constraint and reducer behavior; Screenplay names it portably).
16. **Treat recovered source and observations as evidence requiring review.** Preserve unknowns and classified loss.
17. **Keep planning, publication, capture, AI-assisted interpretation and production operations as separate authority boundaries.** Capability is not consent.
18. **Prefer model-first where an accepted model exists, without forcing it onto framework or brownfield work.** Use repository-native development there.

## Where hand-written code belongs
1. Infrastructure outside generated ownership: adapters, transports, credentials, options, service registration and host composition in unmanaged seams (`Customizations/`).
2. Explicit implementation attachments: behavior the language cannot state, with a modeled requirement, a typed contract and the relevant specifications.
3. Framework and product work, and brownfield work, under the repository's own development practice.

Hand-written code stays linked to intent and reviewable. It never silently overrides the model,
never lives in managed output, and never makes a build invoke AI.

## Rejected anti-patterns
Do not do these, and do not reopen them because a tool or extractor meets framework vocabulary.
- Code-first changes that silently outrank accepted model intent.
- Diagrams or documentation as a substitute for executable semantics.
- Claims of universal low-code, lossless recovery or target parity.
- Adding language syntax because a framework API is popular.
- Saga, aggregate, passive read-model, HTTP, or broker machinery copied into the core vocabulary. Commands, events, read models, reactions, typed identities, deadlines and specifications express these.
- Silent query broadening, authorization weakening, guessed identities or an unprotected-read fallback.
- Generated TODOs, defaults or stubs presented as a successful realization.
- Editing managed output, or mixing generated and user-owned code without a separate contract.
- AI generation during builds or inside the pure render planner.
- Automatic model mutation from recovered code or observed runtime behavior.
- Historical event reinterpretation disguised as a rename or payload cleanup.
- Exactly-once claims inferred from replay controls, delivery ids or reference clock execution.
- Treating a green compile, an attractive board or a closed issue as proof the application works.
- Making the model mandatory for every repository task.

## Finding tiers
Say which tier a finding comes from: **compiler contract** (a named tool and version rejects it
for the mode), **modeling default** (deviate with a recorded reason), or **review question** (a
finding only with a domain consequence).
