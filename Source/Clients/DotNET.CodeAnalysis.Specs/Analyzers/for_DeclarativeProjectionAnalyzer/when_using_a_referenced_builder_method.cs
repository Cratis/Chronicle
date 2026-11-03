// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_DeclarativeProjectionAnalyzer;

public class when_using_a_referenced_builder_method : Specification
{
    Compilation _compilation;
    ImmutableArray<Diagnostic> _diagnostics;

    async Task Establish()
    {
        var builder = (await TestProject.CreateProject("""
            public interface IProjectionBuilderCustom
            {
                void Observe<TEvent>(TEvent value);
            }
            """).GetCompilationAsync())!;
        await using var assembly = new MemoryStream();
        builder.WithAssemblyName("ReferencedBuilder").Emit(assembly).Success.ShouldBeTrue();
        var project = TestProject.CreateProject("""
            namespace Cratis.Chronicle.Events
            {
                public class EventTypeAttribute : System.Attribute {}
            }
            [Cratis.Chronicle.Events.EventType] public class ValidEvent {}
            public class InvalidEvent {}
            public class Usage
            {
                public void Configure(IProjectionBuilderCustom builder)
                {
                    builder.Observe(new ValidEvent());
                    builder.Observe(new InvalidEvent());
                }
            }
            """).AddMetadataReference(MetadataReference.CreateFromImage(assembly.ToArray()));
        _compilation = (await project.GetCompilationAsync())!;
        _compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    }

    async Task Because() => _diagnostics = await _compilation.WithAnalyzers(
        [new CodeAnalysis.Analyzers.DeclarativeProjectionAnalyzer()]).GetAnalyzerDiagnosticsAsync();

    [Fact] void should_only_report_the_unregistered_event() => _diagnostics.Length.ShouldEqual(1);
    [Fact] void should_preserve_the_diagnostic_for_an_inferred_event_on_a_custom_builder() =>
        _diagnostics[0].GetMessage().ShouldEqual("Type 'InvalidEvent' used in declarative projection must be marked with [EventType] attribute");
}
