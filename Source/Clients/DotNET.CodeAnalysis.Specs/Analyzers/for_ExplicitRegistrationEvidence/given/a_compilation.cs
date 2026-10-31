// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ExplicitRegistrationEvidence.given;

public class a_compilation : Specification
{
    protected const string Contracts = """
        using System;
        using System.Collections.Generic;
        namespace Cratis.Chronicle.Registrations
        {
            public class ExplicitArtifacts
            {
                public void RegisterEventType<T>() {}
                public void RegisterEventType(Type eventType) {}
                public void RegisterReactor<T>() {}
                public void RegisterReactor(Type reactorType) {}
                public void RegisterReducer<T>() {}
                public void RegisterReducer(Type reducerType) {}
            }
        }
        namespace Cratis.Chronicle.Events { public class EventTypeAttribute : Attribute {} }
        namespace Cratis.Chronicle.EventSequences
        {
            public interface IEventSequence { void Append(string eventSourceId, object @event); }
        }
        namespace Cratis.Chronicle.Projections
        {
            public interface IProjectionBuilderFor<T> { void From<TEvent>(); }
        }
        namespace Cratis.Chronicle.Projections.ModelBound
        {
            public class FromEventAttribute<T> : Attribute {}
        }
        namespace Cratis.Chronicle.Reactors
        {
            public interface IReactor {}
            public class OnceOnlyAttribute : Attribute {}
        }
        namespace Cratis.Chronicle.Reducers { public interface IReducer {} public interface IReducerFor<T> : IReducer {} }
        public interface IBaseEvent {}
        public sealed record PlainEvent : IBaseEvent;
        public record Model;
        [Cratis.Chronicle.Projections.ModelBound.FromEvent<PlainEvent>]
        public record Projected;
        public class Consumer
        {
            public void Use(Cratis.Chronicle.EventSequences.IEventSequence sequence, Cratis.Chronicle.Projections.IProjectionBuilderFor<Model> projection)
            {
                sequence.Append("source", new PlainEvent());
                projection.From<PlainEvent>();
            }
        }
        public class Reactor : Cratis.Chronicle.Reactors.IReactor
        {
            [Cratis.Chronicle.Reactors.OnceOnly]
            public void On(PlainEvent @event) {}
        }
        public class Reducer : Cratis.Chronicle.Reducers.IReducerFor<Model>
        {
            public Model On(PlainEvent @event, Model? current) => new();
        }
        public class PlainReactor
        {
            public PlainEvent On(IBaseEvent @event) => new();
            public IEnumerable<PlainEvent> Many(PlainEvent @event) => new[] { @event };
        }
        public class PlainReducer
        {
            public Model On(IBaseEvent @event, Model? current) => new();
        }
        """;

    protected static Task<ImmutableArray<Diagnostic>[]> AnalyzeAll(string registration) => Task.WhenAll(
        Analyze(new CodeAnalysis.Analyzers.EventSequenceAppendAnalyzer(), registration),
        Analyze(new CodeAnalysis.Analyzers.DeclarativeProjectionAnalyzer(), registration),
        Analyze(new CodeAnalysis.Analyzers.ModelBoundProjectionAttributeAnalyzer(), registration),
        Analyze(new CodeAnalysis.Analyzers.ReactorMethodAnalyzer(), registration),
        Analyze(new CodeAnalysis.Analyzers.ReducerMethodAnalyzer(), registration));

    protected static async Task<ImmutableArray<Diagnostic>> Analyze(DiagnosticAnalyzer analyzer, string registration, string? contracts = null)
    {
        var project = TestProject.CreateProject(contracts ?? Contracts).AddDocument("Composition.cs", registration).Project;
        var compilation = (await project.GetCompilationAsync())!;
        compilation.GetDiagnostics().Where(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        return await compilation.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync();
    }
}
