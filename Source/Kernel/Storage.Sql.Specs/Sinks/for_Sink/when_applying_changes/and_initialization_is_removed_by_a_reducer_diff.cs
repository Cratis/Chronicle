// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sinks.for_ISink.given;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes;

public class and_initialization_is_removed_by_a_reducer_diff : an_accumulating_read_model<SqlSinkHarness>
{
    IDictionary<string, object?> _stored;

    async Task Establish() => await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 0UL);

    async Task Because()
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        Change[] changes = [new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference(WellKnownProperties.ReadModelInstanceInitialized, true, null)])];
        changeset.Changes.Returns(changes);
        await _sink.ApplyChanges(_key, changeset, 1UL);
        _stored = (await _sink.FindOrDefault(_key))!;
    }

    [Fact] void should_keep_the_row_initialized() => _stored[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(true);
    [Fact] void should_preserve_the_reducer_value() => _stored["count"].ShouldEqual(1);
}
