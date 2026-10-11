// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.when_releasing;

public class and_the_pinned_generation_is_not_in_the_definition : given.a_release_boundary
{
    Exception _error;
    void Establish() => _definition = _definition with { Generations = _definition.Generations.Where(_ => _.Generation.Value != 2).ToArray() };
    async Task Because() => _error = await Catch.Exception(() => _release.Release(_event.Context.EventStore, [_pin], _schemas, [_event]));
    [Fact] void should_fail_explicitly() => _error.ShouldBeOfExactType<PinnedEventTypeGenerationUnavailable>();
}
