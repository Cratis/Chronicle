// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors;

public class when_starting_after_removing_reactors : given.watched_reactors
{
    void Establish() => _artifacts.ReadModelReactors.Returns([]);

    void Because()
    {
        _reactors.Start();
        Emit(ReadModelChangeType.Modified);
    }

    [Fact] void should_dispose_the_old_subscriptions() => _frames.HasObservers.ShouldBeFalse();
    [Fact] void should_not_dispatch_to_the_removed_reactor() => _changes.ShouldBeEmpty();
}
