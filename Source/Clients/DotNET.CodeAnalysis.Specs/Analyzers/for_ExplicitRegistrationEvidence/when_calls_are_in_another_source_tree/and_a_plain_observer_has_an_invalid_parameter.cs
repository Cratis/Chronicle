// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ExplicitRegistrationEvidence.when_calls_are_in_another_source_tree;

public class and_a_plain_observer_has_an_invalid_parameter : given.a_compilation
{
    const string Registration = """
        public class Composition
        {
            public void Configure(Cratis.Chronicle.Registrations.ExplicitArtifacts artifacts)
            {
                artifacts.RegisterEventType<PlainEvent>();
                artifacts.RegisterReactor<PlainReactor>();
                artifacts.RegisterReducer<PlainReducer>();
            }
        }
        """;
    ImmutableArray<Diagnostic> _reactorDiagnostics;
    ImmutableArray<Diagnostic> _reducerDiagnostics;

    async Task Because()
    {
        _reactorDiagnostics = await Analyze(
            new CodeAnalysis.Analyzers.ReactorMethodAnalyzer(),
            Registration,
            Contracts.Replace("public PlainEvent On(IBaseEvent @event)", "public PlainEvent On(IBaseEvent @event, int invalid)"));
        _reducerDiagnostics = await Analyze(
            new CodeAnalysis.Analyzers.ReducerMethodAnalyzer(),
            Registration,
            Contracts.Replace("public Model On(IBaseEvent @event, Model? current) => new();", "public Model On(IBaseEvent @event, Model? current) => new(); public Model Invalid(PlainEvent @event, Model? current, string invalid) => new();"));
    }

    [Fact] void should_check_nominated_reactor_signatures() => _reactorDiagnostics.Select(_ => _.Id).ShouldContainOnly("CHR0004");
    [Fact] void should_check_nominated_reducer_signatures() => _reducerDiagnostics.Select(_ => _.Id).ShouldContainOnly("CHR0006");
}
