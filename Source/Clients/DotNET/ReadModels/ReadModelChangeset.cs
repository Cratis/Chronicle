// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents the changeset for a read model.
/// </summary>
/// <typeparam name="TReadModel">Type of read model.</typeparam>
/// <param name="Namespace">The namespace for the event store.</param>
/// <param name="ModelKey">The <see cref="ModelKey"/> for the model.</param>
/// <param name="ReadModel">The instance of the read model.</param>
/// <param name="Removed">Whether the read model was removed.</param>
/// <param name="ChangeType">The <see cref="ReadModelChangeType"/> that occurred.</param>
/// <param name="ChangeContext">The <see cref="EventContext"/> of the event that caused the change, when available.</param>
public record ReadModelChangeset<TReadModel>(
    EventStoreNamespaceName Namespace,
    ReadModelKey ModelKey,
    TReadModel? ReadModel,
    bool Removed,
    ReadModelChangeType ChangeType = ReadModelChangeType.Modified,
    EventContext? ChangeContext = null);
