// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.for_RegisterReactor.when_serializing;

public class and_a_newer_client_sends_it_to_an_older_kernel : Specification
{
    ReleasedRegisterReactor _result;

    void Because()
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, new RegisterReactor
        {
            ConnectionId = "connection",
            EventStore = "store",
            Namespace = "namespace",
            Reactor = new() { ReactorId = "observer" },
            RegistrationId = Guid.NewGuid()
        });
        stream.Position = 0;
        _result = Serializer.Deserialize<ReleasedRegisterReactor>(stream);
    }

    [Fact] void should_preserve_connection() => _result.ConnectionId.ShouldEqual("connection");
    [Fact] void should_preserve_event_store() => _result.EventStore.ShouldEqual("store");
    [Fact] void should_preserve_namespace() => _result.Namespace.ShouldEqual("namespace");
    [Fact] void should_preserve_reactor() => _result.Reactor.ReactorId.ShouldEqual("observer");
}
