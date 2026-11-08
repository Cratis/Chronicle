// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks.when_getting_for_a_named_sink;

public class and_the_name_resolves_to_another_type : given.named_sinks
{
    ISink _result;

    async Task Because() => _result = await _sinks.GetFor(ReadModel("reporting"));

    [Fact] void should_create_the_sink_of_the_named_configuration() => _result.ShouldEqual(_reportingSink);
    [Fact] void should_not_create_a_sink_on_the_default_connection() => _defaultFactory.DidNotReceive().CreateFor(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.EventStoreNamespaceName>(), Arg.Any<Concepts.ReadModels.ReadModelDefinition>());
}
