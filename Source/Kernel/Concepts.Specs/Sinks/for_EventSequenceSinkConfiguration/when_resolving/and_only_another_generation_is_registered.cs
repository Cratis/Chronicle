// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Concepts.Sinks.for_EventSequenceSinkConfiguration.when_resolving;

public class and_only_another_generation_is_registered : Specification
{
    EventSequenceSinkConfiguration _configuration;
    EventTypeSchema _registered;
    Exception _error;

    void Establish()
    {
        _registered = new(new EventType("PublicStateChanged", new EventTypeGeneration(8)), default, default, new JsonSchema());
        _configuration = new(new EventType("PublicStateChanged", new EventTypeGeneration(7)), "public-feed");
    }

    void Because() => _error = Catch.Exception(() => _configuration.ResolveTarget([_registered]));

    [Fact] void should_not_substitute_the_latest_generation() => _error.ShouldBeOfExactType<MissingEventTargetSchema>();
    [Fact] void should_keep_the_named_destination() => _configuration.Destination.Value.ShouldEqual("public-feed");
}
