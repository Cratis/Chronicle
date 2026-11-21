// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_GraphViewAnalyzer.when_analyzing_types;

public class a_valid_view : given.a_graph_view_analyzer
{
    const string Usage = """
    [ViewOf<Person>] public record PersonWithGroups([Key] Guid Id, string Name, [Follows<MemberOf>] IReadOnlyList<GroupMembership> Groups, [Follows<MemberOf>] GroupMembership? First, [Follows<MemberOf>] GroupMembership[] All);
    [ViewOf<Group>] public record GroupMembership(Guid Id, string Name, [FromEdge] DateTimeOffset Since);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.GraphViewAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
