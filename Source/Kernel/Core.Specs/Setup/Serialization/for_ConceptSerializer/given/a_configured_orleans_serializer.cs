// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Setup.Serialization.for_ConceptSerializer.given;

public class a_configured_orleans_serializer : Specification
{
    protected Serializer _serializer;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new JsonSerializerOptions());
        services.AddConceptSerializer();
        var provider = services.BuildServiceProvider();
        _serializer = provider.GetRequiredService<Serializer>();
    }

    protected T RoundTrip<T>(T value) => _serializer.Deserialize<T>(_serializer.SerializeToArray(value));
}
