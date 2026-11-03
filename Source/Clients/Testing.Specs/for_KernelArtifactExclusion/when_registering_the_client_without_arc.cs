// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.for_KernelArtifactExclusion;

public class when_registering_the_client_without_arc : Specification
{
    WebApplicationBuilder _builder;
    IServiceCollection _services;

    void Establish()
    {
        _builder = WebApplication.CreateBuilder();
        _services = _builder.Services;
    }

    void Because() => _builder.AddCratisChronicle();

    [Fact] void should_not_add_arc_command_aggregates() => _services.Any(_ => _.ServiceType == typeof(ICommandHandlerProviders)).ShouldBeFalse();
    [Fact] void should_not_add_arc_query_aggregates() => _services.Any(_ => _.ServiceType == typeof(IQueryPerformerProviders)).ShouldBeFalse();
    [Fact] void should_not_add_arc_type_discovery() => _services.Any(_ => _.ServiceType == typeof(ITypes)).ShouldBeFalse();
}
