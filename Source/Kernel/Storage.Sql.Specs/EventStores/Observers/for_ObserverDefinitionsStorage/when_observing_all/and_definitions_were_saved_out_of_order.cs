// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Observers.for_ObserverDefinitionsStorage.when_observing_all;

public class and_definitions_were_saved_out_of_order : given.an_observing_observer_definitions_storage
{
    async Task Because()
    {
        await Seed("b");
        await Seed("c");
        await Seed("a");
        await Subscribe();
    }

    [Fact] void should_emit_definitions_ordered_by_identifier() => _emissions.Single().Select(_ => _.Identifier.Value).ShouldEqual(["a", "b", "c"]);
}
