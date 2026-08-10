// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker;

public class ReactorWithSyncEventsWithConcurrencyScopesReturnType
{
    public EventsWithConcurrencyScopes Handle(MyEvent @event) => new([], []);
}
