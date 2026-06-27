// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkParity;

[Collection(MongoDBCollection.Name)]
public class when_a_reducer_removes_a_child(MongoDBFixture fixture) : given.a_parity_scenario(fixture)
{
    protected override Type ReadModelType => typeof(Team);

    protected override IReadOnlyList<Func<ExpandoObject>> States =>
    [
        () => CreateTeam("member-1", "member-2", "member-3"),
        () => CreateTeam("member-1", "member-3")
    ];

    [Fact] void should_apply_identically_across_sinks() => ParityReport.ShouldEqual(string.Empty);

    static ExpandoObject CreateTeam(params string[] memberIds) =>
        Expando(
            ("id", "root-1"),
            ("members", memberIds.Select(id => (object)Expando(("memberId", id), ("status", "active"))).ToArray()));

    record Member(string MemberId, string Status);
    record Team(string Id, IEnumerable<Member> Members);
}
