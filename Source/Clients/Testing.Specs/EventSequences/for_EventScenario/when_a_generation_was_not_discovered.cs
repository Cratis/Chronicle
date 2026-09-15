// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_a_generation_was_not_discovered : Specification
{
    Exception _error;

    void Because()
    {
        var defaults = Defaults.Instance;
        var storage = new InMemoryEventTypesStorage(() => defaults.EventTypes, defaults.JsonSchemaGenerator);

        // The kernel's concept types are private package dependencies, not part of the consumer API.
        var method = typeof(InMemoryEventTypesStorage).GetMethods().Single(method => method.Name == "GetFor" && method.GetParameters().Length == 2);
        var parameters = method.GetParameters();
        var id = Activator.CreateInstance(parameters[0].ParameterType, defaults.EventTypes.GetEventTypeFor(typeof(ContactReclassified)).Id.Value);
        var generation = Activator.CreateInstance(parameters[1].ParameterType, 999U);
        _error = Catch.Exception(() => method.Invoke(storage, [id, generation]))?.GetBaseException();
    }

    [Fact] void should_not_substitute_the_latest_schema() => _error.ShouldBeOfExactType<EventSchemaNotDiscovered>();
    [Fact] void should_identify_the_event() => _error.Message.ShouldContain(nameof(ContactReclassified));
    [Fact] void should_identify_the_missing_generation() => _error.Message.ShouldContain("999");
}
