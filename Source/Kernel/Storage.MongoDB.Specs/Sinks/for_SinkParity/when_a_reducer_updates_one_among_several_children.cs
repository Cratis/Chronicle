// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkParity;

[Collection(MongoDBCollection.Name)]
public class when_a_reducer_updates_one_among_several_children(MongoDBFixture fixture) : given.a_parity_scenario(fixture)
{
    protected override Type ReadModelType => typeof(Team);

    protected override IReadOnlyList<Func<ExpandoObject>> States =>
    [
        () => CreateTeam(("member-1", "active"), ("member-2", "active"), ("member-3", "active")),
        () => CreateTeam(("member-1", "active"), ("member-2", "promoted"), ("member-3", "active"))
    ];

    [Fact] void should_apply_identically_across_sinks() => ParityReport.ShouldEqual(string.Empty);

    static ExpandoObject CreateTeam(params (string Id, string Status)[] members) =>
        Expando(
            ("id", "root-1"),
            ("members", members.Select(m => (object)Expando(("memberId", m.Id), ("status", m.Status))).ToArray()));

    record Member(string MemberId, string Status);
    record Team(string Id, IEnumerable<Member> Members);
}
