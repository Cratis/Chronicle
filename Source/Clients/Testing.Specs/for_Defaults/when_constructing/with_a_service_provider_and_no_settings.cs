// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.for_Defaults.when_constructing;

public class with_a_service_provider_and_no_settings : Specification
{
    Defaults _defaults;

    void Because() => _defaults = new(new InMemoryClientArtifactsProvider(), new ServiceCollection().BuildServiceProvider());

    [Fact] void should_leave_scenarios_to_resolve_their_own_settings() => _defaults.ScenarioOptions.ShouldBeNull();
}
