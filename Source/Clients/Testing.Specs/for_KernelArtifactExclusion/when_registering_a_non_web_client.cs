// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Testing.for_KernelArtifactExclusion;

public class when_registering_a_non_web_client : Specification
{
    HostApplicationBuilder _builder;

    void Establish() => _builder = Host.CreateApplicationBuilder();

    void Because()
    {
        _builder.AddCratisChronicle();
        _builder.AddCratisChronicle();
    }

    [Fact] void should_discover_and_register_the_command_collection_once() => _builder.Services.Count(_ => _.ServiceType == typeof(IInstancesOf<ICommandHandlerProvider>)).ShouldEqual(1);
    [Fact] void should_discover_and_register_the_query_collection_once() => _builder.Services.Count(_ => _.ServiceType == typeof(IInstancesOf<IQueryPerformerProvider>)).ShouldEqual(1);
    [Fact] void should_not_add_arc_command_aggregates() => _builder.Services.Any(_ => _.ServiceType == typeof(ICommandHandlerProviders)).ShouldBeFalse();
    [Fact] void should_not_add_arc_query_aggregates() => _builder.Services.Any(_ => _.ServiceType == typeof(IQueryPerformerProviders)).ShouldBeFalse();
}
