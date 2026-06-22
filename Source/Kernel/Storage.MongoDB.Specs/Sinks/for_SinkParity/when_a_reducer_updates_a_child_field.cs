// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkParity;

[Collection(MongoDBCollection.Name)]
public class when_a_reducer_updates_a_child_field(when_a_reducer_updates_a_child_field.context ctx) : IClassFixture<when_a_reducer_updates_a_child_field.context>
{
    public class context(MongoDBFixture fixture) : given.a_parity_scenario(fixture)
    {
        protected override Type ReadModelType => typeof(Team);

        protected override IReadOnlyList<Func<ExpandoObject>> States =>
        [
            () => CreateTeam("pending"),
            () => CreateTeam("approved")
        ];

        static ExpandoObject CreateTeam(string status) =>
            Expando(
                ("id", "root-1"),
                ("members", new object[] { Expando(("memberId", "member-1"), ("status", status)) }));

        record Member(string MemberId, string Status);
        record Team(string Id, IEnumerable<Member> Members);
    }

    [Fact] void should_apply_identically_across_sinks() => ctx.ParityReport.ShouldEqual(string.Empty);
    [Fact] void should_have_a_result_from_both_sinks() => (ctx.InMemoryResult is not null && ctx.MongoResult is not null).ShouldBeTrue();
}
