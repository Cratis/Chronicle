// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Grpc.Core;
using ProtoBuf.Grpc.Configuration;

namespace Cratis.Chronicle.Connections.for_LazyServices.given;

public class recording_client_factory : ClientFactory
{
    public ConcurrentQueue<(Type ServiceType, CallInvoker CallInvoker, object Service)> Creations { get; } = new();

    public Exception Failure { get; set; }

    public Type? FailingServiceType { get; set; }

    public Action? OnCreated { get; set; }

    protected override BinderConfiguration BinderConfiguration => BinderConfiguration.Default;

    public override TService CreateClient<TService>(CallInvoker channel)
    {
        if (typeof(TService) == FailingServiceType)
        {
            throw Failure;
        }

        var service = Substitute.For<TService>();
        Creations.Enqueue((typeof(TService), channel, service));
        OnCreated?.Invoke();
        return service;
    }
}
