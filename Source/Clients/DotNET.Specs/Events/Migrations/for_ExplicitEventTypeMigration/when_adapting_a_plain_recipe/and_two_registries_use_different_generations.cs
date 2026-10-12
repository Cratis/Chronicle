// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_ExplicitEventTypeMigration.when_adapting_a_plain_recipe;

public class and_two_registries_use_different_generations : given.a_plain_migration
{
    IEventTypes _other;
    IEventTypeMigration _first;
    IEventTypeMigration _second;
    IEventTypeMigration _secondInReverseOrder;
    IEventTypeMigration _firstInReverseOrder;

    void Establish()
    {
        _other = Substitute.For<IEventTypes>();
        _other.GetEventTypeFor(typeof(Previous)).Returns(new EventType("other", 4));
        _other.GetEventTypeFor(typeof(Upgrade)).Returns(new EventType("other", 5));
    }

    void Because()
    {
        _first = _methods.Create(_services, _eventTypes);
        _second = _methods.Create(_services, _other);
        _secondInReverseOrder = _methods.Create(_services, _other);
        _firstInReverseOrder = _methods.Create(_services, _eventTypes);
    }

    [Fact] void should_preserve_the_first_registry_generation() => _first.To.ShouldEqual((EventTypeGeneration)2);
    [Fact] void should_preserve_the_second_registry_generation() => _second.To.ShouldEqual((EventTypeGeneration)5);
    [Fact] void should_preserve_the_first_registry_in_reverse_order() => _firstInReverseOrder.To.ShouldEqual((EventTypeGeneration)2);
    [Fact] void should_preserve_the_second_registry_in_reverse_order() => _secondInReverseOrder.To.ShouldEqual((EventTypeGeneration)5);
    [Fact] void should_inject_the_first_owning_registry() => _activation.Received(2).Record(_eventTypes);
    [Fact] void should_inject_the_second_owning_registry() => _activation.Received(2).Record(_other);
}
