// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ExplicitRegistrationEvidence.when_calls_are_in_another_source_tree;

public class and_typeof_calls_nominate_events_and_observers : given.a_compilation
{
    const string Registration = """
        public class Composition
        {
            public void Configure(Cratis.Chronicle.Registrations.ExplicitArtifacts artifacts)
            {
                artifacts.RegisterEventType(typeof(PlainEvent));
                artifacts.RegisterReactor(typeof(PlainReactor));
                artifacts.RegisterReducer(typeof(PlainReducer));
            }
        }
        """;

    ImmutableArray<Diagnostic>[] _diagnostics;

    async Task Because() => _diagnostics = await AnalyzeAll(Registration);

    [Fact] void should_accept_appending() => _diagnostics[0].ShouldBeEmpty();
    [Fact] void should_accept_fluent_projection_events() => _diagnostics[1].ShouldBeEmpty();
    [Fact] void should_accept_model_bound_projection_events() => _diagnostics[2].ShouldBeEmpty();
    [Fact] void should_accept_reactor_parameters_and_returned_events() => _diagnostics[3].ShouldBeEmpty();
    [Fact] void should_accept_reducer_exact_and_polymorphic_events() => _diagnostics[4].ShouldBeEmpty();
}
