// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Observation;

namespace Basic;

[Observer("faaa186b-eb58-4a25-adef-fdd82c07da6b")]
public class MyObserver
{
    public Task MyEventOccurred(MyEvent @event, EventContext context)
    {
        Console.WriteLine($"MyEvent occurred at {context.Occurred} - {@event}");
        return Task.CompletedTask;
    }
}
