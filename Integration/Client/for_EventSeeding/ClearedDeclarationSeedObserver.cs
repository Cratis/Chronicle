// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_EventSeeding;

public class ClearedDeclarationSeedObserver : IReactor
{
    public TaskCompletionSource RemovedSeedObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource RetainedSeedObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Opened(OfficeOpened @event, EventContext context)
    {
        if (context.Namespace == EventStoreNamespaceName.Default && @event.Name == "Removed")
        {
            RemovedSeedObserved.TrySetResult();
        }
        if (context.Namespace.Value == "future" && @event.Name == "Retained")
        {
            RetainedSeedObserved.TrySetResult();
        }
    }
}
