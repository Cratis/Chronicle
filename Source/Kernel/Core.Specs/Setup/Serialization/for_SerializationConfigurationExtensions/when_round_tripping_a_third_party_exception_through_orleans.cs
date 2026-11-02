// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage;
using Cratis.Orleans;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Orleans.Serialization;
using Orleans.Serialization.Invocation;

namespace Cratis.Chronicle.Setup.Serialization.for_SerializationConfigurationExtensions;

public class when_round_tripping_a_third_party_exception_through_orleans : Specification
{
    Exception _original;
    Exception _result;

    void Establish()
    {
        try
        {
            throw new NpgsqlException("connection refused");
        }
        catch (NpgsqlException ex)
        {
            _original = ex;
        }
    }

    void Because()
    {
        var services = new ServiceCollection();
        services.AddCratisOrleansSerializers();
        services.AddSingleton(Substitute.For<IExpandoObjectConverter>());
        services.AddSingleton(Substitute.For<IStorage>());
        var builder = Substitute.For<ISiloBuilder>();
        builder.Services.Returns(services);
        builder.ConfigureSerialization();
        using var provider = services.BuildServiceProvider();
        var serializer = provider.GetRequiredService<Serializer>();
        using var response = Response.FromException(_original);
        using var result = serializer.Deserialize<Response>(serializer.SerializeToArray(response));
        _result = result.Exception;
    }

    [Fact] void should_keep_the_exception_type() => _result.ShouldBeOfExactType<NpgsqlException>();
    [Fact] void should_keep_the_message() => _result.Message.ShouldEqual(_original.Message);
}
