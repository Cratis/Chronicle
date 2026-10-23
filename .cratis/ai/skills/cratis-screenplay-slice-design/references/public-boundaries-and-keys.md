<!-- cratis-ai-managed: skills/cratis-screenplay-slice-design/references/public-boundaries-and-keys.md -->
# Public boundaries and view identity

Public boundaries and view identity in slice design.

Translate crosses an application boundary: inbound consumes foreign public events
or data and produces private facts; outbound folds private facts into exactly one
local public event type. Commands never publish public events. Put direction in
the slice outline and trace every exported field; read automations-and-translations
`references/public-events-and-direction.md` for the released ESM v9 contract.

Mark view identity with trailing `key`. Use a composite key for independent view
coordinates (resource + period), a natural single id for one entity's state.
Supply every named part in single-instance queries and command/trigger reads;
see read-surface `references/named-keys.md`. Flat composite view keys bind as ESM v27
(older binders refuse with PLAY0268; compiler requirement in named-keys), as do composite
stream routes. Command and trigger reads by parts stay refused, and Stage does not admit
v27. Keep the true identity rather than inventing a concatenated id; choosing between
shapes: streams-and-consistency `references/read-model-keys.md`.
