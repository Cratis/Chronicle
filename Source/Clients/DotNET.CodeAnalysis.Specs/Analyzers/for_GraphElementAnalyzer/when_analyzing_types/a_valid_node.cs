// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_GraphElementAnalyzer.when_analyzing_types;

public class a_valid_node : given.a_graph_element_analyzer
{
    const string Usage = """
    public record Address(string Street);
    [Node] public record Person([Key] Guid PersonId, string Name, int[] Scores, DateTimeOffset Born, List<string> Tags, Guid? Manager);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.GraphElementAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
