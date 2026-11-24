// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors;

public class when_starting_after_discovering_reactors : given.watched_reactors
{
    void Establish()
    {
        _reactors.Stop();
        _artifacts.ReadModelReactors.Returns([]);
        _reactors.Start();
        _artifacts.ReadModelReactors.Returns([typeof(Reactor)]);
    }

    void Because()
    {
        _reactors.Start();
        Emit(ReadModelChangeType.Added);
    }

    [Fact] void should_dispatch_to_the_discovered_reactor() => _changes.ShouldEqual([ReadModelChangeType.Added]);
}
