// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcherManager.given;

public class a_projection_watcher_manager : Specification
{
    protected IReadModelWatcherFactory _projectionWatcherFactory;
    protected ReadModelWatcherManager _manager;

    void Establish()
    {
        _projectionWatcherFactory = Substitute.For<IReadModelWatcherFactory>();
        _manager = new ReadModelWatcherManager(_projectionWatcherFactory);
    }
}
