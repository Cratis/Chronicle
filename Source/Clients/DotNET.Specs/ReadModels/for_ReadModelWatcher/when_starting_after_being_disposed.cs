// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcher;

public class when_starting_after_being_disposed : given.a_watcher
{
    void Because()
    {
        _watcher.Dispose();
        _watcher.Start();
    }

    [Fact] void should_not_watch() => _readModelsService.DidNotReceive().Watch(Arg.Any<WatchRequest>());
}
