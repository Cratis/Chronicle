// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors;

public class when_dispatching_kernel_changes : given.watched_reactors
{
    void Because()
    {
        Emit(ReadModelChangeType.Added);
        Emit(ReadModelChangeType.Modified);
        Emit(ReadModelChangeType.Removed);
        Emit(ReadModelChangeType.Added);
    }

    [Fact] void should_dispatch_each_authoritative_change() => _changes.ShouldEqual([ReadModelChangeType.Added, ReadModelChangeType.Modified, ReadModelChangeType.Removed, ReadModelChangeType.Added]);
    [Fact] void should_preserve_each_event_context() => _contexts.ShouldEqual(Enumerable.Repeat(_context, 4));
}
