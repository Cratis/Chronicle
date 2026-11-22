// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Carries a <see cref="Schemas.JsonSchema"/> between grains as its JSON text.
/// </summary>
/// <remarks>
/// A schema is a graph of mutable NJsonSchema-style objects that Orleans cannot generate a serializer for, so the text is the
/// wire form. The receiver parses it once per schema it receives.
/// </remarks>
/// <param name="Json">The schema as JSON.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Setup.Serialization.JsonSchemaSurrogate")]
public readonly record struct JsonSchemaSurrogate([property: Id(0)] string Json);
