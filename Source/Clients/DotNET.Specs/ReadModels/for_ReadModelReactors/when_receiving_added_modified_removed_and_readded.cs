// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors;

public class when_receiving_added_modified_removed_and_readded : given.watched_reactors
{
    async Task Because()
    {
        await Send(ReadModelChangeType.Added);
        await Send(ReadModelChangeType.Modified);
        await Send(ReadModelChangeType.Removed);
        await Send(ReadModelChangeType.Added);
    }

    [Fact] void should_dispatch_each_supplied_change_type() => _recorded.Select(_ => _.Type).ShouldEqual([ReadModelChangeType.Added, ReadModelChangeType.Modified, ReadModelChangeType.Removed, ReadModelChangeType.Added]);
    [Fact] void should_preserve_each_context() => _recorded.TrueForAll(_ => _.Context == _context).ShouldBeTrue();
}
