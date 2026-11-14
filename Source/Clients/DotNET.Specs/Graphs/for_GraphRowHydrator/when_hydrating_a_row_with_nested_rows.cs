// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs.for_GraphRowHydrator;

public class when_hydrating_a_row_with_nested_rows : Specification
{
    static readonly Guid _teamId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    static readonly Guid _memberId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public enum Level
    {
        Junior = 0,
        Senior = 1
    }

    public record TeamName(string Value) : ConceptAs<string>(Value);

    public record Member(Guid Id, TeamName? Name, Level Level, DateTimeOffset Since, int? Age, decimal Score);

    public record Team(Guid Id, TeamName Name, IReadOnlyList<Member> Members, Member? Lead, IReadOnlyList<string> Tags);

    GraphRow _row;
    Team _team;

    void Establish()
    {
        var member = new GraphRow(new Dictionary<string, object?>
        {
            ["id"] = _memberId.ToString(),
            ["level"] = "Senior",
            ["since"] = "2026-01-01T00:00:00Z",
            ["age"] = null,
            ["score"] = 1.5d
        });
        _row = new GraphRow(new Dictionary<string, object?>
        {
            ["id"] = _teamId.ToString(),
            ["name"] = "Platform",
            ["members"] = new List<GraphRow> { member },
            ["lead"] = null,
            ["tags"] = new List<object?> { "a", "b" }
        });
    }

    void Because() => _team = new GraphRowHydrator().Hydrate<Team>(_row)!;

    [Fact] void should_read_a_guid_from_its_string() => _team.Id.ShouldEqual(_teamId);
    [Fact] void should_build_a_concept_from_the_value_it_wraps() => _team.Name.ShouldEqual(new TeamName("Platform"));
    [Fact] void should_hydrate_the_nested_rows() => _team.Members.Single().Id.ShouldEqual(_memberId);
    [Fact] void should_read_an_enum_from_its_name() => _team.Members[0].Level.ShouldEqual(Level.Senior);
    [Fact] void should_read_a_timestamp_from_its_iso_string() => _team.Members[0].Since.ShouldEqual(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    [Fact] void should_read_a_decimal_from_a_number() => _team.Members[0].Score.ShouldEqual(1.5m);
    [Fact] void should_give_null_for_an_absent_nullable_value() => _team.Members[0].Age.ShouldBeNull();
    [Fact] void should_give_null_for_an_absent_property() => _team.Members[0].Name.ShouldBeNull();
    [Fact] void should_give_null_for_a_relation_of_one_without_an_item() => _team.Lead.ShouldBeNull();
    [Fact] void should_read_a_list_of_primitives() => _team.Tags.ShouldContainOnly("a", "b");
}
