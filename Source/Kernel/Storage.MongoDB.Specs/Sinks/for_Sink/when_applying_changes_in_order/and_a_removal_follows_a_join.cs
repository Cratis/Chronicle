// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_in_order;

public class and_a_removal_follows_a_join : given.a_sink_recording_writes
{
    void Establish() => _changes = [Direct(10), Join(20), new ChildRemovedFromAll(new PropertyPath("[children]"), new PropertyPath("id"), "child", ArrayIndexers.NoIndexers)];

    async Task Because() => await Apply();

    [Fact] void should_keep_the_removal_after_the_join() => _writes.ShouldEqual(["direct:10", "join:20", "remove"]);
}
