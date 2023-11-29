// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;

namespace Basic;

public class ConnectionStatus : IParticipateInConnectionLifecycle
{
    public Task ClientConnected()
    {
        Console.WriteLine("Horse");
        return Task.CompletedTask;
    }

    public Task ClientDisconnected()
    {
        return Task.CompletedTask;
    }
}
