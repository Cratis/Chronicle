// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Changes;
using Cratis.Events;
using Cratis.Kernel.Storage.Changes;

namespace Cratis.Kernel.Storage.MongoDB.Projections;

/// <summary>
/// Represents a <see cref="IChangesetStorage"/> for storing changesets in MongoDB.
/// </summary>
public class ChangesetStorage : IChangesetStorage
{
    /// <inheritdoc/>
    public Task Save(CorrelationId correlationId, IChangeset<AppendedEvent, ExpandoObject> associatedChangeset)
    {
        return Task.CompletedTask;
    }
}
