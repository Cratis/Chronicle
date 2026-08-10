// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Monads;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// An <see cref="IReactorSideEffectHandlers"/> that records the side effects a reactor returns — events and commands
/// alike — for assertion, instead of appending or executing them. Used by <see cref="ReactorScenario{TReactor}"/> when
/// no explicit handlers are supplied, so a return-style reactor can be asserted directly.
/// </summary>
sealed class RecordingReactorSideEffectHandlers : IReactorSideEffectHandlers
{
    readonly List<object> _produced = [];

    /// <summary>
    /// Gets the side effects the reactor produced, flattened — collections are expanded and
    /// <see cref="EventForEventSourceId"/> wrappers are unwrapped to the underlying event.
    /// </summary>
    public IReadOnlyList<object> Produced => _produced;

    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, object value)
    {
        if (reactorContext.EventStore is not null)
        {
            return CanHandle(reactorContext, reactorContext.EventStore, value);
        }

        return true;
    }

    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, IEventStore eventStore, object value) => true;

    /// <inheritdoc/>
    public Task<Result<ReactorSideEffectFailure>> Handle(ReactorContext reactorContext, IEventStore eventStore, object value)
    {
        _produced.AddRange(Flatten(value));
        return Task.FromResult(Result.Success<ReactorSideEffectFailure>());
    }

    static IEnumerable<object> Flatten(object value)
    {
        switch (value)
        {
            case EventForEventSourceId forEventSourceId:
                yield return forEventSourceId.Event;
                break;

            case IEnumerable<object> collection:
                foreach (var item in collection)
                {
                    foreach (var inner in Flatten(item))
                    {
                        yield return inner;
                    }
                }

                break;

            default:
                yield return value;
                break;
        }
    }
}
