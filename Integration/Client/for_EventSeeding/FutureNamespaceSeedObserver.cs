// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_EventSeeding;

public class FutureNamespaceSeedObserver : IReactor
{
    public TaskCompletionSource SeedObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Opened(OfficeOpened @event, EventContext context)
    {
        if (context.Namespace.Value == "future")
        {
            SeedObserved.TrySetResult();
        }
    }
}
