// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_ReminderEntryConverters.when_bounding_keys;

public class and_the_composite_fits : Specification
{
    string _key;

    void Because() => _key = ReminderEntryConverters.GetRowKey(GrainId.Create("observer", "short"), "retry");

    [Fact] void should_preserve_the_existing_row_key() => _key.ShouldEqual("observer/short-retry");
}
