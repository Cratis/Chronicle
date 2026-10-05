// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Setup.Serialization.for_JsonSerializedTypes;

public class when_round_tripping_a_cratis_exception_through_orleans : Specification
{
    static readonly ConnectionId _connectionId = new("6d884b84-f045-4df5-b6d7-f56fa2f7a953");
    Exception _original;
    Exception _result;

    void Establish()
    {
        // A thrown exception carries a TargetSite, which System.Text.Json cannot write.
        try
        {
            throw new ClientIsNotConnected(_connectionId);
        }
        catch (ClientIsNotConnected ex)
        {
            _original = ex;
        }
    }

    void Because()
    {
        var services = new ServiceCollection();
        services.AddExceptionSerialization();
        services.AddSerializer(builder => builder.AddJsonSerializer(JsonSerializedTypes.Includes, new JsonSerializerOptions()));
        var serializer = services.BuildServiceProvider().GetRequiredService<Serializer>();
        _result = serializer.Deserialize<Exception>(serializer.SerializeToArray(_original));
    }

    [Fact] void should_keep_the_exception_type() => _result.ShouldBeOfExactType<ClientIsNotConnected>();
    [Fact] void should_keep_the_message() => _result.Message.ShouldContain(_connectionId.ToString());
}
