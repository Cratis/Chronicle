// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_writing_a_composite_key;

public class and_a_join_has_no_target_key_parts : given.a_composite_key_sink
{
    string[] _stamps;
    int _updated;

    protected override ISqlSinkHarness CreateHarness() => new SqlSinkHarness();

    async Task Because()
    {
        await Write("a", "b", "group");
        await Write("c", "d", "group");
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("UPDATE composite_shadows SET __compositeKey=NULL WHERE id='a_b'");
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Changes.Returns([new Joined(new ExpandoObject(), "group", "name", ArrayIndexers.NoIndexers, [new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("name", null, "joined")])])]);
        await _sink.ApplyChanges(new Key("group", ArrayIndexers.NoIndexers), changeset, EventSequenceNumber.First);
        _stamps = await context.Database.SqlQuery<string>($"SELECT COALESCE(__compositeKey, 'unstamped') AS Value FROM composite_shadows ORDER BY id").ToArrayAsync();
        _updated = await context.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM composite_shadows WHERE name='joined'").SingleAsync();
    }

    [Fact] void should_update_both_join_targets() => _updated.ShouldEqual(2);
    [Fact] void should_leave_the_unstamped_target_null() => _stamps[0].ShouldEqual("unstamped");
    [Fact] void should_never_overwrite_the_other_targets_stamp() => _stamps[1].ShouldEqual("~k1:1:c1:d");
}
