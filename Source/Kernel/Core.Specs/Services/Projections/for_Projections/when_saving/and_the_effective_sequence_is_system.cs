// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.ReadModels;
using Cratis.Monads;
using Grpc.Core;

using Catch = Cratis.Specifications.Catch;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_saving;

public class and_the_effective_sequence_is_system : given.all_dependencies
{
    readonly List<Exception> _errors = [];
    bool _graph;

    void Establish()
    {
        _languageService.IsGraphDeclaration(Arg.Any<string>()).Returns(_ => _graph);
        _languageService.CompileGraph(Arg.Any<string>(), Arg.Any<ProjectionOwner>(), Arg.Any<IEnumerable<EventTypeSchema>>(), Arg.Any<Concepts.Observation.EventGenerationDelivery>())
            .Returns(Result.Success<CompiledGraph, CompilerErrors>(new(_projectionDefinition, _readModelDefinition)));
    }

    async Task Because()
    {
        foreach (var graph in new[] { false, true })
        {
            _graph = graph;
            foreach (var sequence in new[] { "system", "SYSTEM", " SyStEm ", "\tSyStEm\r\n" })
            {
                _errors.Add(await Catch.Exception(() => _service.Save(new()
                {
                    EventStore = EventStore,
                    Namespace = EventStoreNamespace,
                    EventSequenceId = sequence,
                    Declaration = "projection Test => test-read-model"
                })));
            }
        }
    }

    [Fact] void should_refuse_every_variant_for_both_declaration_forms() => _errors.Count.ShouldEqual(8);
    [Fact] void should_use_the_existing_refusal() => _errors.TrueForAll(_ => _ is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldBeTrue();
    [Fact] void should_leave_the_compiled_event_log_definition_unchanged() => _projectionDefinition.EventSequenceId.IsEventLog.ShouldBeTrue();
    [Fact] void should_not_register_a_projection() => _grainFactory.DidNotReceive().GetGrain<IProjectionsManager>(Arg.Any<string>());
    [Fact] void should_not_register_a_read_model() => _grainFactory.DidNotReceive().GetGrain<IReadModelsManager>(Arg.Any<string>());
}
