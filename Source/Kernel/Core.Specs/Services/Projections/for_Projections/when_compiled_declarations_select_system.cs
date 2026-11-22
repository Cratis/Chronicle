// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.Schemas;
using Cratis.Monads;
using Grpc.Core;

using Catch = Cratis.Specifications.Catch;
using CompilerModels = Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.given;

namespace Cratis.Chronicle.Services.Projections.for_Projections;

public class when_compiled_declarations_select_system : given.all_dependencies
{
    readonly List<Exception> _errors = [];
    readonly List<ProjectionDefinition> _compiled = [];
    ILanguageService _compiler;

    void Establish()
    {
        _compiler = new LanguageService(new Generator(), Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.given.ProjectionCodeGenerators.All());
        _readModelDefinition = _readModelDefinition with
        {
            Identifier = "UserGroupReadModel",
            Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = JsonSchema.FromType<CompilerModels.UserGroupReadModel>() }
        };
        SetReadModels(_readModelDefinition);
        _storage.GetEventStore(EventStore).EventTypes.GetLatestForAllEventTypes().Returns(
        [
            new EventTypeSchema((EventType)"UserAdded", EventTypeOwner.Client, EventTypeSource.Code, JsonSchema.FromType<CompilerModels.UserAdded>()),
            new EventTypeSchema((EventType)"GroupCreated", EventTypeOwner.Client, EventTypeSource.Code, JsonSchema.FromType<CompilerModels.GroupCreated>())
        ]);
        _languageService.Compile(Arg.Any<string>(), Arg.Any<ProjectionOwner>(), Arg.Any<IEnumerable<ReadModelDefinition>>(), Arg.Any<IEnumerable<EventTypeSchema>>(), Arg.Any<EventGenerationDelivery>()).Returns(Compile);
    }

    Result<ProjectionDefinition, CompilerErrors> Compile(CallInfo call)
    {
        var result = _compiler.Compile(call.ArgAt<string>(0), call.ArgAt<ProjectionOwner>(1), call.ArgAt<IEnumerable<ReadModelDefinition>>(2), call.ArgAt<IEnumerable<EventTypeSchema>>(3), call.ArgAt<EventGenerationDelivery>(4));
        var definition = result.Match(_ => _, errors => throw new InvalidOperationException(string.Join(", ", errors.Errors)));
        _compiled.Add(definition);
        return result;
    }

    async Task Because()
    {
        foreach (var sequence in new[] { "system", "SYSTEM", " SyStEm ", "\tSyStEm\r\n" })
        {
            var declaration = $$"""
                projection SequenceSelection => UserGroupReadModel
                  sequence {{sequence.Trim()}}
                  join Group on groupId
                    with GroupCreated
                      automap
                """;
            _errors.Add(await Catch.Exception(() => _service.Preview(new() { EventStore = EventStore, Namespace = EventStoreNamespace, EventSequenceId = "log", Declaration = declaration })));
            _errors.Add(await Catch.Exception(() => _service.Save(new() { EventStore = EventStore, Namespace = EventStoreNamespace, EventSequenceId = "log", Declaration = declaration })));
        }

        await ReadCompiledGraphAndRegistration();
    }

    async Task ReadCompiledGraphAndRegistration()
    {
        _languageService.IsGraphDeclaration(Arg.Any<string>()).Returns(true);
        _languageService.CompileGraph(Arg.Any<string>(), Arg.Any<ProjectionOwner>(), Arg.Any<IEnumerable<EventTypeSchema>>(), Arg.Any<EventGenerationDelivery>())
            .Returns(Result.Success<CompiledGraph, CompilerErrors>(new(_compiled[^1], _readModelDefinition)));
        _errors.Add(await Catch.Exception(() => _service.Save(new() { EventStore = EventStore, Namespace = EventStoreNamespace, EventSequenceId = "log", Declaration = "graph SequenceSelection" })));
        _errors.Add(await Catch.Exception(() => _service.Register(new()
        {
            EventStore = EventStore,
            Projections = [new() { Identifier = "SequenceSelection", ReadModel = "UserGroupReadModel", EventSequenceId = _compiled[^1].EventSequenceId.Value }]
        })));
    }

    [Fact] void should_compile_the_real_declarations() => _compiled.Count.ShouldEqual(8);
    [Fact] void should_include_the_join_in_every_compiled_definition() => _compiled.TrueForAll(_ => _.Join.Count == 1).ShouldBeTrue();
    [Fact] void should_select_system_in_every_compiled_definition() => _compiled.TrueForAll(_ => _.EventSequenceId.IsSystemSequence()).ShouldBeTrue();
    [Fact] void should_check_preview_save_graph_save_and_registration() => _errors.Count.ShouldEqual(10);
    [Fact] void should_refuse_the_compiled_sequence_despite_the_event_log_request() => _errors.TrueForAll(_ => _ is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldBeTrue();
}
