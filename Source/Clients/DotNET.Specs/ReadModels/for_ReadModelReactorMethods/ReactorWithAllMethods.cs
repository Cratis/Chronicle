// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactorMethods;

public class ReactorWithAllMethods : IReadModelReactor
{
    public Task Added(WatchedReadModel model) => Task.CompletedTask;

    public Task Modified(IEnumerable<WatchedReadModel> models) => Task.CompletedTask;

    public void Removed(WatchedReadModel model)
    {
    }

    public Task NotAHandler(WatchedReadModel model) => Task.CompletedTask;
}
