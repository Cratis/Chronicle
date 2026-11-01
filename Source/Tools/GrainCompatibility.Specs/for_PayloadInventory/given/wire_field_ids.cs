// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Session;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.given;

public static class wire_field_ids
{
    public static uint[] Of<T>(T value)
    {
        var services = new ServiceCollection();
        services.AddSerializer(builder => builder.AddAssembly(typeof(payload_types).Assembly));
        using var provider = services.BuildServiceProvider();
        var bytes = provider.GetRequiredService<Serializer>().SerializeToArray(value);
        using var session = provider.GetRequiredService<SerializerSessionPool>().GetSession();
        var reader = Reader.Create(bytes, session);
        reader.ReadFieldHeader();
        var ids = new List<uint>();
        uint id = 0;
        while (true)
        {
            var field = reader.ReadFieldHeader();
            if (field.IsEndBaseOrEndObject)
            {
                return ids.ToArray();
            }

            id += field.FieldIdDelta;
            ids.Add(id);
            reader.SkipField(field);
        }
    }
}
