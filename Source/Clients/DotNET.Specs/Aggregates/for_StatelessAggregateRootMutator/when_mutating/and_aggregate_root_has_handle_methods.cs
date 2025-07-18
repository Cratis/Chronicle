// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_StatelessAggregateRootMutator.when_mutating;

public class and_aggregate_root_has_handle_methods : given.a_stateless_aggregate_root_mutator
{
    string _event;

    void Establish()
    {
        _eventHandlers.HasHandleMethods.Returns(true);
        _event = Guid.NewGuid().ToString();
    }

    async Task Because() => await _mutator.Mutate(_event);

    [Fact] void should_handle_event() => _eventHandlers.Received().Handle(_aggregateRoot, Arg.Is<IEnumerable<EventAndContext>>(arg => arg.Select(_ => _.Event).SequenceEqual(new[] { _event })));
}
