// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactions;

namespace Basic;

public class MyObserver : IReaction
{
    public Task MyEventHandler(MyEvent @event)
    {
        Console.WriteLine("Got event");
        return Task.CompletedTask;
    }
}
