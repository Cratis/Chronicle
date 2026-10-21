// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReducerMethodAnalyzer.when_analyzing_reducer_methods;

public class and_event_parameter_is_from_a_referenced_assembly : given.a_reducer_method_analyzer
{
    Compilation _compilation;
    ImmutableArray<Diagnostic> _diagnostics;

    async Task Establish()
    {
        var events = await TestProject.CreateProject(CreateSource("""
        public interface IImportedEvent {}
        [EventType] public class ImportedEvent : IImportedEvent {}
        [EventType] public class BaseEvent {}
        public class InheritedEvent : BaseEvent {}
        public interface IImportedPreviousEvent {}
        [Cratis.Chronicle.Events.EventTypeGenerationFor<ImportedEvent>(1)]
        public class ImportedPreviousEvent : IImportedPreviousEvent {}
        """)).GetCompilationAsync();
        await using var assembly = new MemoryStream();
        events!.WithAssemblyName("ReferencedEvents").Emit(assembly).Success.ShouldBeTrue();
        var reference = MetadataReference.CreateFromImage(assembly.ToArray());
        var project = TestProject.CreateProject(CreateSource("""
        public class Model {}
        public class Reducer : Cratis.Chronicle.Reducers.IReducerFor<Model>
        {
            public Model Reduce(IImportedEvent @event, Model? current) => current ?? new Model();
            public Model Reduce(InheritedEvent @event, Model? current) => current ?? new Model();
            public Model Reduce(ImportedPreviousEvent @event, Model? current) => current ?? new Model();
            public Model Reduce(IImportedPreviousEvent @event, Model? current) => current ?? new Model();
        }
        """)).AddMetadataReference(reference);
        _compilation = (await project.GetCompilationAsync())!;
        _compilation.GetDiagnostics().Where(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    }

    async Task Because() => _diagnostics = await _compilation.WithAnalyzers(
        [new CodeAnalysis.Analyzers.ReducerMethodAnalyzer()]).GetAnalyzerDiagnosticsAsync();

    [Fact] void should_accept_handlers_for_imported_event_types() => _diagnostics.ShouldBeEmpty();
}
