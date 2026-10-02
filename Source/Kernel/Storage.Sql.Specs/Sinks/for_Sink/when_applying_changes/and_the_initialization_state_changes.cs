// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sinks.for_ISink.given;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes;

public class and_the_initialization_state_changes : an_accumulating_read_model<SqlSinkHarness>
{
    IDictionary<string, object?> _placeholder;
    IDictionary<string, object?> _initialized;
    IDictionary<string, object?> _updated;

    async Task Because()
    {
        await _sink.ApplyChanges(_key, SettingInitialized(false), 0UL);
        _placeholder = (await _sink.FindOrDefault(_key))!;
        await _sink.ApplyChanges(_key, SettingInitialized(true), 1UL);
        _initialized = (await _sink.FindOrDefault(_key))!;
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(42), 2UL);
        _updated = (await _sink.FindOrDefault(_key))!;
    }

    [Fact] void should_store_the_placeholder_as_uninitialized() => _placeholder[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(false);
    [Fact] void should_store_the_root_as_initialized() => _initialized[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(true);
    [Fact] void should_preserve_initialization_on_later_updates() => _updated[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(true);
    [Fact] void should_leave_unprojected_placeholder_properties_absent() => _placeholder.ContainsKey("count").ShouldBeFalse();

    static IChangeset<AppendedEvent, ExpandoObject> SettingInitialized(bool initialized)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        Change[] changes = [new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference(WellKnownProperties.ReadModelInstanceInitialized, null, initialized)])];
        changeset.Changes.Returns(changes);
        return changeset;
    }
}
