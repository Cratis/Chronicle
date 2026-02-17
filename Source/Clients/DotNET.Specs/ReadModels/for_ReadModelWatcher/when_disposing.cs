// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcher;

public class when_disposing : given.a_watcher
{
    void Because() => _watcher.Dispose();

    [Fact] void should_stop_watching() => _stopCount.ShouldEqual(1);
}
