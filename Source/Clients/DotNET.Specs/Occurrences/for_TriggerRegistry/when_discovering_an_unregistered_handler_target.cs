// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.for_TriggerRegistry;

public class when_discovering_an_unregistered_handler_target : given.an_occurrence_client
{
    void Establish()
    {
        _artifacts.Triggers.Returns([]);
        _artifacts.OccurrenceHandlers.Returns([typeof(given.TestOccurrenceHandler)]);
    }

    void Because() => _error = Catch.Exception(_registry.Discover);

    [Fact] void should_refuse_the_handler() => _error.ShouldBeOfExactType<TriggerNotValid>();
}
