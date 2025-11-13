// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Represents the definition of a specific generation of an event type.
/// </summary>
/// <param name="Generation">The <see cref="EventTypeGeneration"/> this definition is for.</param>
/// <param name="Schema">The <see cref="JsonSchema"/> for this generation.</param>
public record EventTypeGenerationDefinition(EventTypeGeneration Generation, JsonSchema Schema);
