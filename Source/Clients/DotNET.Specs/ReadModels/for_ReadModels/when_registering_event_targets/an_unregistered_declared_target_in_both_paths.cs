// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering_event_targets;

public class an_unregistered_declared_target_in_both_paths : given.a_registered_unmarked_target<an_unregistered_declared_target_in_both_paths.Target>
{
    void Establish()
    {
        _eventTypes.HasFor(typeof(Target)).Returns(false);
        _eventTypes.GetEventTypeFor(typeof(Target)).Returns(_ => throw new TypeIsNotAnEventType(typeof(Target)));
    }

    async Task Because() => await RegisterBoth();

    [Fact] void should_reject_bulk_registration() => _bulkError.ShouldBeOfExactType<TypeIsNotAnEventType>();
    [Fact] void should_reject_generic_registration() => _genericError.ShouldBeOfExactType<TypeIsNotAnEventType>();
    [Fact] void should_not_register_an_ordinary_read_model_instead() => _definitions.ShouldBeEmpty();

    [EventType]
    public record Target(string Name);
}
