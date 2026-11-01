// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.for_RegisterReactor.when_serializing;

public class and_an_older_client_omits_the_registration_id : Specification
{
    RegisterReactor _result;

    void Because()
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, new ReleasedRegisterReactor
        {
            ConnectionId = "connection",
            EventStore = "store",
            Namespace = "namespace",
            Reactor = new() { ReactorId = "observer" }
        });
        stream.Position = 0;
        _result = Serializer.Deserialize<RegisterReactor>(stream);
    }

    [Fact] void should_leave_registration_id_null() => _result.RegistrationId.ShouldBeNull();
    [Fact] void should_preserve_connection() => _result.ConnectionId.ShouldEqual("connection");
    [Fact] void should_preserve_event_store() => _result.EventStore.ShouldEqual("store");
    [Fact] void should_preserve_namespace() => _result.Namespace.ShouldEqual("namespace");
    [Fact] void should_preserve_reactor() => _result.Reactor.ReactorId.ShouldEqual("observer");
}
