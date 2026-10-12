// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ExplicitRegistrationEvidence.when_registration_cannot_be_proved;

public class and_a_call_is_conditional : given.a_compilation
{
    const string Registration = """
        public class Composition
        {
            public void Configure(Cratis.Chronicle.Registrations.ExplicitArtifacts artifacts, bool execute)
            {
                if (execute) artifacts.RegisterEventType<PlainEvent>();
            }
        }
        """;

    ImmutableArray<Diagnostic> _diagnostics;

    async Task Because() => _diagnostics = await Analyze(new CodeAnalysis.Analyzers.EventSequenceAppendAnalyzer(), Registration);

    [Fact] void should_treat_a_possible_registration_as_evidence_not_execution_proof() => _diagnostics.ShouldBeEmpty();
}
