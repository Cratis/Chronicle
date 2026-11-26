<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/read-model-property-order.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Read-model property order (ESM 37.1)

Requires Screenplay 5.6.0 or later (`screenplay --version`). Read-model declaration order is optional,
ignorable metadata and is **not** preserved by default: the CLI, MCP, TypeScript compiler and editors
never request it, and it adds no `.play` syntax or diagnostic. A C# host opts in with
`SemanticModelBinder.Bind(name, document, documents, new SemanticBindingOptions(PreserveReadModelPropertyOrder: true))`,
which returns a `SemanticBindingOptionsResult` (`Compilation`, `Refusal`, `Success`).

- Only when the option is used and removing the metadata leaves a valid 37.0 model that reads the
  application calendar or a clock does the model select language/semantic **37.1**. `schemaVersion`
  stays 37 and each read model gains an optional `propertyOrder` after `properties`: a permutation of
  every property identity, key included. `properties` stays id-sorted.
- A model that emits no metadata (no read models, or every read model already in canonical property
  order) keeps its version and binds successfully. Any other model that is not a 37.0 candidate gets
  `PropertyOrderMetadataRefusal` with no executable value. Default `Bind` and default options keep existing versions, bytes and
  revisions.
- The golden is `full-esm-v37.1.json`, reached with `EsmGoldenVectors.GetMinor(SemanticVersion.V37_1)`;
  `Get(int)` and `Versions` describe only `.0` goldens.
- A 37.0 consumer that reads through the [unknown-preserving reader](unknown-preserving-reader.md)
  keeps the member as opaque bytes with the same revision.

Do not tell modelers that declaration order controls a generated constructor's parameter order: that
rendering is a Stage change; confirm it shipped before claiming otherwise.
