<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/compliance.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Personal data, concepts and operational secrets

Verified at `Cratis/Chronicle.Elixir v3.10.2`: `Source/chronicle/lib/chronicle/{concept,compliance,confidentiality}.ex`, `events/event_type.ex:92-93`, and `read_models/read_model.ex`.

`pii/1,2` marks personal data for compliance encryption and right-to-erasure. `encrypted/1,2,3` (since 3.5.0) marks non-personal operational secrets for separate confidentiality encryption with no compliance erasure operation. Its scopes are `:subject` (default), `:namespace`, `:global`. They are not interchangeable.

```elixir
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

defmodule MyApp.PersonName do
  use Chronicle.Concept, type: :string
  pii("Legal name of the data subject")
end

defmodule MyApp.Events.PartnerIntegrationConfigured do
  use Chronicle.Events.EventType, id: "partner-integration-configured"
  defstruct contact_name: %MyApp.PersonName{}, email: "", api_key: ""

  pii(:email, "Partner contact's email address")
  encrypted(:api_key, :subject, "Partner API key")
end
```

`Chronicle.Concept` ships on Hex from **3.0.0**, not 2.2.0 (the 2.3.0 tag was unpublished). `type:` is required: `:string`, `:integer`, `:float`, `:boolean`, `:uuid`. Typed concept defaults let the schema generator carry the wrapped type and classification onto every leaf that uses it. The concept's `Jason.Encoder` serializes only its wrapped value.

On a concept use `pii/0,1` to classify its sole value, not the field-oriented `Chronicle.Compliance.pii/1,2`. Concept `encrypted/0,1,2` similarly marks that value. Neither classification is allowed on a concept with `event_source_id: true`; use a non-sensitive surrogate stream identity instead. Both classifications on one schema leaf raise `Chronicle.Confidentiality.PiiAndEncryptedCombinedNotSupported`.

For a read model whose subject is not its `:id`, declare `subject(:customer_id)` in the module using `Chronicle.ReadModels.ReadModel`. It identifies the natural person for PII release; there is only one subject declaration per module. `subject/1` is not imported by `EventType`; append-time `:subject` is an append option, not this read-model macro.
