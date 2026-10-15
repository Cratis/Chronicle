// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Sinks.for_EventSequenceSinkFactory.when_creating;

public class and_the_target_carries_compliance : Specification
{
    Exception _error;

    async Task Because()
    {
        var configuration = new EventSequenceSinkConfiguration(new EventType("Changed", EventTypeGeneration.First), null, true);
        var schema = await JsonSchema.FromJsonAsync("{\"type\":\"object\",\"properties\":{\"Name\":{\"type\":\"string\",\"compliance\":[{\"type\":\"PII\",\"details\":\"\"}]}}}");
        var definition = new ReadModelDefinition(
            "target",
            "target",
            "Target",
            ReadModelOwner.None,
            ReadModelSource.Unknown,
            ReadModelObserverType.NotSet,
            ReadModelObserverIdentifier.Unspecified,
            new(SinkConfigurationId.None, WellKnownSinkTypes.EventSequence, configuration),
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema },
            []);
        _error = Catch.Exception(() => new EventSequenceSinkFactory(Substitute.For<IServiceProvider>()).CreateFor("store", "tenant", definition));
    }

    [Fact] void should_refuse_rather_than_publish_unprotected() => _error.ShouldBeOfExactType<EventSequenceTargetCarriesCompliance>();
}
