// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents the changeset for a projection.
/// </summary>
/// <typeparam name="TModel">Type of model the projection is for.</typeparam>
/// <param name="Namespace">The namespace for the event store.</param>
/// <param name="ModelKey">The <see cref="ModelKey"/> for the model.</param>
/// <param name="Model">The instance of the model.</param>
public record ProjectionChangeset<TModel>(EventStoreNamespaceName Namespace, ReadModelKey ModelKey, TModel Model);
