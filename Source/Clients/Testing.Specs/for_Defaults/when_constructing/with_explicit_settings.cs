// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.for_Defaults.when_constructing;

public class with_explicit_settings : Specification
{
    ChronicleOptions _options;
    Defaults _defaults;

    void Establish() => _options = new ChronicleOptions();

    void Because() => _defaults = new(new InMemoryClientArtifactsProvider(), new ServiceCollection().BuildServiceProvider(), _options);

    [Fact] void should_hand_the_settings_to_scenarios() => _defaults.ScenarioOptions.ShouldEqual(_options);
}
