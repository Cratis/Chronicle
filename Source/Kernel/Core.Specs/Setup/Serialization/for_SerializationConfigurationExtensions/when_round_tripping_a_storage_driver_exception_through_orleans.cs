// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage;
using Cratis.Orleans;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Orleans.Serialization;
using Orleans.Serialization.Invocation;

namespace Cratis.Chronicle.Setup.Serialization.for_SerializationConfigurationExtensions;

public class when_round_tripping_a_storage_driver_exception_through_orleans : Specification
{
    Exception _original;
    Exception _result;

    void Establish()
    {
        try
        {
            throw new MongoWaitQueueFullException("The wait queue for acquiring a connection to server localhost:27017 is full.");
        }
        catch (MongoWaitQueueFullException ex)
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

    [Fact] void should_keep_the_exception_type() => _result.ShouldBeOfExactType<MongoWaitQueueFullException>();
    [Fact] void should_keep_the_message() => _result.Message.ShouldEqual(_original.Message);
    [Fact] void should_be_retried_by_startup_steps() => SiblingSiloInstability.IsTransient(_result).ShouldBeTrue();
}
