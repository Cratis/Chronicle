<!-- cratis-ai-managed: skills/cratis-screenplay-streams-and-consistency/references/product-sources.md -->
# Product sources

Sources behind the stream and consistency rules.

## Product sources

Screenplay (`identifier`, `for`, constraints, generations, `id` pins and their diagnostics),
Chronicle (CHR0012, CHR0034 and the constraint and concurrency behavior below), Arc
(`[ProtectedDecision]`, `DecisionRead<T>`) and Stage (rendered apps pin the Arc and
Chronicle client/kernel listed in the Stage skill's `references/rendered-application.md`;
protected-read APIs are available to gap-fill). Tool versions: `cratis-screenplay-toolchain`
`SKILL.md`. Re-check the Chronicle limits below before relying on a constraint in a new release.
