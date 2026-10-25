// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_looking_up_by_composite_key;

public class and_the_row_is_stamped_for_another_key : given.a_composite_key_lookup_table
{
    async Task Establish()
    {
        await Write("x_y", "z", "owner");
        await Write("x", "y_z", "merged");
    }
    async Task Because() => _result = await Lookup("x", "y_z");
    [Fact] void should_report_the_legacy_id_collision() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.KeyCollision);
    [Fact] void should_not_return_an_instance() => _result.Instance.ShouldBeNull();
}
