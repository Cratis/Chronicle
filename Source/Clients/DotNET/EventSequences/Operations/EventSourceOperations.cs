// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.Operations;

/// <summary>
/// Represents an implementation of <see cref="IEventSourceOperations"/> for managing operations related to a specific event source.
/// </summary>
public class EventSourceOperations : IEventSourceOperations
{
    readonly List<IEventSequenceOperation> _operations = [];

    /// <inheritdoc/>
    public IEnumerable<IEventSequenceOperation> Operations => _operations;

    /// <inheritdoc/>
    public ConcurrencyScope ConcurrencyScope => ConcurrencyScope.NotSet;

    /// <inheritdoc/>
    public EventSourceOperations WithConcurrencyScope(Action<ConcurrencyScopeBuilder> configure)
    {
        var builder = new ConcurrencyScopeBuilder();
        configure(builder);
        return this;
    }

    /// <inheritdoc/>
    public EventSourceOperations Append(object @event, Causation? causation = default)
    {
        _operations.Add(new AppendOperation(@event, causation));
        return this;
    }

    /// <inheritdoc/>
    public IEnumerable<object> GetAppendedEvents() =>
        _operations.OfType<AppendOperation>().Select(op => op.Event).ToArray();
}
