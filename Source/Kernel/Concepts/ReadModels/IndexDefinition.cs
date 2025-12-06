// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.ReadModels;

/// <summary>
/// Represents the definition of an index on a read model property.
/// </summary>
/// <param name="PropertyPath">The property path to index.</param>
[GenerateSerializer]
[Alias(nameof(IndexDefinition))]
public record IndexDefinition(PropertyPath PropertyPath);
