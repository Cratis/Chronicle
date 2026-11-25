// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Carries a <see cref="System.Text.Json.Nodes.JsonObject"/> between grains as its JSON text.
/// </summary>
/// <param name="Json">The object as JSON.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Setup.Serialization.JsonObjectSurrogate")]
public readonly record struct JsonObjectSurrogate([property: Id(0)] string Json);
