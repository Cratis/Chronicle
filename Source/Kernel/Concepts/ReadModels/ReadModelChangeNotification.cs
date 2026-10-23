// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Concepts.ReadModels;

/// <summary>
/// Represents a read model instance and the change to notify watchers of.
/// </summary>
/// <param name="ReadModelKey">The key identifying the read model instance.</param>
/// <param name="ReadModel">The serialized read model.</param>
/// <param name="Change">The context describing the change.</param>
[GenerateSerializer]
[Alias(nameof(ReadModelChangeNotification))]
public record ReadModelChangeNotification(
    [property: Id(0)] ReadModelKey ReadModelKey,
    [property: Id(1)] JsonObject ReadModel,
    [property: Id(2)] ReadModelChangeContext Change);
