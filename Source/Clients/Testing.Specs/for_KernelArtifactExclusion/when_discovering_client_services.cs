// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.for_KernelArtifactExclusion;

public class when_discovering_client_services : given.an_application_host
{
    void Because() => BuildHost(false);

    [Fact] void should_discover_the_testing_hook() => _application.Services.GetRequiredService<ITypes>().FindMultiple<IChronicleClientServices>().ShouldContain(typeof(KernelArtifactExclusion));
    [Fact] void should_apply_the_hook_to_command_providers() => _application.Services.GetRequiredService<IInstancesOf<ICommandHandlerProvider>>().GetType().DeclaringType.ShouldEqual(typeof(KernelArtifactExclusion));
    [Fact] void should_apply_the_hook_to_query_providers() => _application.Services.GetRequiredService<IInstancesOf<IQueryPerformerProvider>>().GetType().DeclaringType.ShouldEqual(typeof(KernelArtifactExclusion));
}
