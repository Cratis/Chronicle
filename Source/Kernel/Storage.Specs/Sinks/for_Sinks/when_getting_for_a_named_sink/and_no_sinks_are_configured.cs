// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks.when_getting_for_a_named_sink;

public class and_no_sinks_are_configured : Specification
{
    Exception _error;

    async Task Because()
    {
        var factory = Substitute.For<ISinkFactory>();
        factory.TypeId.Returns(WellKnownSinkTypes.MongoDB);
        var sinks = new Sinks(string.Empty, string.Empty, new KnownInstancesOf<ISinkFactory>([factory]));
        var readModel = new ReadModelDefinition(
            "orders",
            "orders",
            "Orders",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB) { Name = "reporting" },
            new Dictionary<ReadModelGeneration, JsonSchema>(),
            []);
        _error = await Catch.Exception(() => sinks.GetFor(readModel));
    }

    [Fact] void should_refuse_the_unknown_name() => _error.ShouldBeOfExactType<UnknownSinkName>();
}
