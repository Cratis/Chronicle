// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Models;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents the changeset for a projection.
/// </summary>
/// <param name="Namespace">The namespace for the event store.</param>
/// <param name="ModelKey">The <see cref="ModelKey"/> for the model.</param>
/// <param name="Model">The instance of the model.</param>
public record ProjectionChangeset(EventStoreNamespaceName Namespace, ModelKey ModelKey, ExpandoObject Model);
