// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ExplicitRegistrationEvidence.when_registration_cannot_be_proved;

public class and_an_unrelated_method_has_the_same_name : given.a_compilation
{
    const string Registration = """
        public class Unrelated
        {
            public void RegisterEventType<T>() {}
            public void Configure() => RegisterEventType<PlainEvent>();
        }
        """;

    ImmutableArray<Diagnostic>[] _diagnostics;

    async Task Because() => _diagnostics = await AnalyzeAll(Registration);

    [Fact] void should_still_report_appending() => _diagnostics[0].Select(_ => _.Id).ShouldContainOnly("CHR0001");
    [Fact] void should_still_report_fluent_projection_events() => _diagnostics[1].Select(_ => _.Id).ShouldContainOnly("CHR0002");
    [Fact] void should_still_report_model_bound_projection_events() => _diagnostics[2].Select(_ => _.Id).ShouldContainOnly("CHR0003");
    [Fact] void should_still_report_marked_reactor_intent() => _diagnostics[3].Select(_ => _.Id).ShouldContainOnly("CHR0005");
    [Fact] void should_still_report_reducer_events() => _diagnostics[4].Select(_ => _.Id).ShouldContainOnly("CHR0007");
}
