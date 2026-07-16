// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Setup.Serialization.for_OneOfSerializer.given;

public class a_configured_orleans_serializer : Specification
{
    protected Serializer _serializer;

    void Establish()
    {
        var options = new JsonSerializerOptions();
        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddConceptSerializer();
        services.AddSingleton(Substitute.For<Cratis.Chronicle.Json.IExpandoObjectConverter>());
        services.AddSingleton(Substitute.For<Cratis.Chronicle.Storage.IStorage>());
        services.AddCustomSerializers();
        services.AddSerializer(builder => builder.AddJsonSerializer(type => type.Namespace == "OneOf.Types", options));
        var provider = services.BuildServiceProvider();
        _serializer = provider.GetRequiredService<Serializer>();
    }

    protected T RoundTrip<T>(T value) => _serializer.Deserialize<T>(_serializer.SerializeToArray(value));
}
