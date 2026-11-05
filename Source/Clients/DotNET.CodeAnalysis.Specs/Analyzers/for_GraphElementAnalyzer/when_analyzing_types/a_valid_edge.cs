// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_GraphElementAnalyzer.when_analyzing_types;

public class a_valid_edge : given.a_graph_element_analyzer
{
    const string Usage = """
    [Edge("MEMBER_OF")] public record MemberOf(Guid PersonId, Guid GroupId, DateTimeOffset Since);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.GraphElementAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
