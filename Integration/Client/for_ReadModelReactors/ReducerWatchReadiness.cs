// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ReadModelReactors;

public static class ReducerWatchReadiness
{
    public static Task WaitFor(IEventStore store, CancellationToken cancellationToken) =>
        store.ReadModels.GetWatcherFor<ReducedModel>().Subscribed.WaitAsync(cancellationToken);
}
