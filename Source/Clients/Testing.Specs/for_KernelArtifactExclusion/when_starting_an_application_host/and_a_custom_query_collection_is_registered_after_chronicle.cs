// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.for_KernelArtifactExclusion.when_starting_an_application_host;

public class and_a_custom_query_collection_is_registered_after_chronicle : given.an_application_host
{
    Exception? _startupFailure;

    void Establish() => BuildHost(
        chronicleBeforeArc: false,
        configureServices: services => services.AddSingleton<IInstancesOf<IQueryPerformerProvider>>(provider => new InstancesOf<IQueryPerformerProvider>(provider.GetRequiredService<ITypes>(), provider)));

    async Task Because() => _startupFailure = await Catch.Exception(() => _application.StartAsync());

    [Fact] void should_fail_startup_with_our_check() => _startupFailure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_name_the_custom_collection() => _startupFailure!.Message.ShouldContain(typeof(IInstancesOf<IQueryPerformerProvider>).ToString());
    [Fact] void should_explain_how_to_register_providers() => _startupFailure!.Message.ShouldContain("register individual ICommandHandlerProvider or IQueryPerformerProvider implementations instead");
}
