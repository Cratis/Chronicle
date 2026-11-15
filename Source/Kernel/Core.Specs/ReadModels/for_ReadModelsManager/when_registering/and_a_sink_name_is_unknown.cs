// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_a_sink_name_is_unknown : given.a_read_models_manager
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _manager.Register(
    [
        DefinitionFor("orders", "Orders") with { Sink = new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB) { Name = "reporting" } }
    ]));

    [Fact] void should_refuse_the_read_model() => _error.ShouldBeOfExactType<UnknownSinkName>();
    [Fact] void should_name_the_read_model() => _error.Message.ShouldContain("'orders'");
    [Fact] void should_name_the_sink() => _error.Message.ShouldContain("'reporting'");
    [Fact] async Task should_not_register_any_definition() => (await _manager.GetDefinitions()).ShouldBeEmpty();
}
