<!-- cratis-ai-managed: skills/cratis-screenplay-slice-design/references/identity-edits.md -->
# Identity-affecting edits on an existing model

How to classify an edit against catalog addresses.

Classify an edit by whether it changes catalog addresses. When `.screenplay/identities.json`
exists, adding, removing or renaming an addressed element (declarations, properties of commands, events,
read models, composite types and triggers, queries, query arguments, specifications), moving
declarations between files, creating, deleting, renaming or moving a mapped `.play` document, and any
contract change to a persisted event go through MCP, performed by the identity owner
(`cratis-screenplay-modeling-lifecycle`, *Classify by catalog address*; evolution classes:
`cratis-screenplay-streams-and-consistency`). Bounded text edits are only for edits that
preserve addresses (descriptions, rule and expression bodies, mappings between existing
members). Without `identities.json`, plain text edits are fine. Never use a text edit to get
around a refusal from the typed tools.
