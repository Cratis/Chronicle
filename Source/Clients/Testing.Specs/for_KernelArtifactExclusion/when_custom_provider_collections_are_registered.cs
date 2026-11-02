// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.for_KernelArtifactExclusion;

public class when_custom_provider_collections_are_registered : Specification
{
    ServiceCollection _commands;
    ServiceCollection _queries;
    Exception _commandError;
    Exception _queryError;

    void Establish()
    {
        _commands = new ServiceCollection();
        _commands.AddSingleton(Substitute.For<IInstancesOf<ICommandHandlerProvider>>());
        _queries = new ServiceCollection();
        _queries.AddSingleton(Substitute.For<IInstancesOf<IQueryPerformerProvider>>());
    }

    void Because()
    {
        _commandError = Catch.Exception(() => _commands.ConfigureChronicleClientServices());
        _queryError = Catch.Exception(() => _queries.ConfigureChronicleClientServices());
    }

    [Fact] void should_reject_custom_command_collections() => _commandError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_reject_custom_query_collections() => _queryError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_explain_how_to_register_command_providers() => _commandError.Message.ShouldContain("register individual ICommandHandlerProvider or IQueryPerformerProvider implementations instead");
    [Fact] void should_explain_how_to_register_query_providers() => _queryError.Message.ShouldContain("register individual ICommandHandlerProvider or IQueryPerformerProvider implementations instead");
}
