// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_resolving_read_model_dependencies;

public class with_an_unseeded_custom_key : given.a_read_model_scenario<given.KeyResolvingReadModelReactor>
{
    async Task Because() =>
        _exception = await Catch.Exception(() => _scenario.Given.ForEventSource(_source).Events(new VibeStarted("related-key")));

    [Fact] void should_fail_with_the_named_exception() => _exception.ShouldBeOfExactType<ReadModelNotSeededForReactorScenario>();
    [Fact] void should_name_the_custom_key() => _exception!.Message.Contains("'related-key'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_use_the_event_source_as_key() => _exception!.Message.Contains("'triggering-source'", StringComparison.Ordinal).ShouldBeFalse();
}
