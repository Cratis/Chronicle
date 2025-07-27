// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents the changeset for a projection.
/// </summary>
/// <param name="Namespace">The namespace for the event store.</param>
/// <param name="ReadModelKey">The <see cref="ReadModelKey"/> for the model.</param>
/// <param name="ReadModel">The instance of the read model as JSON.</param>
public record ProjectionChangeset(EventStoreNamespaceName Namespace, ReadModelKey ReadModelKey, JsonObject ReadModel);
