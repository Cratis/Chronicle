// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Changes;
using Cratis.Events;

namespace Cratis.Chronicle.Storage.Changes;

/// <summary>
/// Represents a null <see cref="IChangesetStorage"/> that does nothing.
/// </summary>
public class NullChangesetStorage : IChangesetStorage
{
    /// <inheritdoc/>
    public Task Save(CorrelationId correlationId, IChangeset<AppendedEvent, ExpandoObject> associatedChangeset) => Task.CompletedTask;
}
