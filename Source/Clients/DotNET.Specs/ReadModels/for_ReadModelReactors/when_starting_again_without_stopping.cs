// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors;

public class when_starting_again_without_stopping : given.watched_reactors
{
    void Because()
    {
        _reactors.Start();
        Emit(ReadModelChangeType.Modified);
    }

    [Fact] void should_dispatch_the_change_only_once() => _changes.ShouldEqual([ReadModelChangeType.Modified]);
}
