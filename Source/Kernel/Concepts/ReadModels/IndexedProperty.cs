// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.ReadModels;

/// <summary>
/// Represents one ordered property of an index.
/// </summary>
/// <param name="Path">The indexed property path.</param>
/// <param name="Descending">Whether the property is indexed in descending order.</param>
[GenerateSerializer]
[Alias(nameof(IndexedProperty))]
public record IndexedProperty([property: Id(0)] PropertyPath Path, [property: Id(1)] bool Descending = false);
