// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Represents the changeset for a reducer.
/// </summary>
/// <typeparam name="TReadModel">Type of read model the reducer is for.</typeparam>
/// <param name="Namespace">The namespace for the event store.</param>
/// <param name="ModelKey">The <see cref="ModelKey"/> for the model.</param>
/// <param name="ReadModel">The instance of the read model.</param>
/// <param name="Removed">Whether the read model was removed.</param>
public record ReducerChangeset<TReadModel>(EventStoreNamespaceName Namespace, ReadModelKey ModelKey, TReadModel? ReadModel, bool Removed);
