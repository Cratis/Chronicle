// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors.when_watching_a_legacy_local_reducer;

public class and_the_first_change_is_not_a_removal : given.watched_reactors
{
    async Task Because() => await Send(ReadModelChangeType.Modified);

    [Fact] void should_dispatch_modified_instead_of_guessing_added() => _recorded.Select(_ => _.Type).ShouldEqual([ReadModelChangeType.Modified]);
}
