// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Client;

using System.Collections.Immutable;
using Client::Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>
/// Minimal <see cref="IEventTypes"/> that resolves every event type to <see cref="BenchmarkPayload"/>.
/// </summary>
sealed class BenchmarkEventTypes : IEventTypes
{
    /// <inheritdoc/>
    public IImmutableList<Type> AllClrTypes => throw new NotSupportedException();

    /// <inheritdoc/>
    public IImmutableList<EventType> All => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task Discover() => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task Register() => throw new NotSupportedException();

    /// <inheritdoc/>
    public bool HasFor(EventTypeId eventTypeId) => true;

    /// <inheritdoc/>
    public Type GetClrTypeFor(EventTypeId eventTypeId) => typeof(BenchmarkPayload);

    /// <inheritdoc/>
    public Type GetClrTypeFor(EventTypeId eventTypeId, EventTypeGeneration generation) => typeof(BenchmarkPayload);

    /// <inheritdoc/>
    public Cratis.Chronicle.Schemas.JsonSchema GetSchemaFor(EventTypeId eventTypeId) => throw new NotSupportedException();

    /// <inheritdoc/>
    public bool HasFor(Type clrType) => throw new NotSupportedException();

    /// <inheritdoc/>
    public EventType GetEventTypeFor(Type clrType) => throw new NotSupportedException();
}
