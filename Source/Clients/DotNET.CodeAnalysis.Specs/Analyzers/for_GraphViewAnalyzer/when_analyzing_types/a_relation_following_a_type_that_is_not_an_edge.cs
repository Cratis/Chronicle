// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_GraphViewAnalyzer.when_analyzing_types;

public class a_relation_following_a_type_that_is_not_an_edge : given.a_graph_view_analyzer
{
    const string Usage = """
    public record Friendship(Guid PersonId, Guid FriendId);
    [ViewOf<Person>] public record Friend(Guid Id);
    [ViewOf<Person>] public record PersonWithFriends(Guid Id, [Follows<Friendship>] IReadOnlyList<Friend> {|#0:Friends|});
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.GraphViewAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.GraphViewRelationMustFollowEdge, DiagnosticSeverity.Error, "Friends", "PersonWithFriends", "Friendship"));

    [Fact] Task should_report_the_edge() => _result;
}
