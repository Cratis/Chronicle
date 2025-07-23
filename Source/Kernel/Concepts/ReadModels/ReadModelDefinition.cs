// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;

namespace Cratis.Chronicle.Concepts.ReadModels;

/// <summary>
/// Represents the model used by a projection to project to.
/// </summary>
/// <param name="Name">Name of the model.</param>
/// <param name="Schema">The <see cref="JsonSchema"/> for the model.</param>
public record ReadModelDefinition(ReadModelName Name, JsonSchema Schema);
