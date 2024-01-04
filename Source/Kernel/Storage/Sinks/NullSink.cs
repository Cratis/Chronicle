// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Sinks;

namespace Aksio.Cratis.Kernel.Storage.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISink"/> that does nothing.
/// </summary>
public class NullSink : ISink
{
    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.Null;

    /// <inheritdoc/>
    public SinkTypeName Name => "Null sink";

    /// <inheritdoc/>
    public Task ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task BeginReplay() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task EndReplay() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<ExpandoObject?> FindOrDefault(Key key) => Task.FromResult<ExpandoObject?>(null);

    /// <inheritdoc/>
    public Task PrepareInitialRun() => Task.CompletedTask;
}
