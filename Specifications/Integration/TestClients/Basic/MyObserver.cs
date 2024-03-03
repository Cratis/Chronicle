// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Observation;

namespace Basic;

[Observer("bf9d2ee4-5557-43e0-9d95-ad8aa9f4013e")]
public class MyObserver
{
    public Task MyEventHandler(MyEvent @event)
    {
        Console.WriteLine("Got event");
        return Task.CompletedTask;
    }
}
