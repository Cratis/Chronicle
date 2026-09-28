// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Setup.Serialization.for_ExpandoObjectSerializer.given;

/// <summary>
/// The kernel's Orleans serializers, as a message crossing a silo boundary goes through them.
/// </summary>
public class a_serializer_for_expando_objects : Specification
{
    protected Serializer _serializer;
    protected Serializer _previousSerializer;

    void Establish()
    {
        _serializer = CreateSerializer(_ => { });

        // A silo that has not been upgraded yet: the codec as it was, the first one registered for the type.
        _previousSerializer = CreateSerializer(builder => builder.Services.AddCompleteSerializer<PreviousExpandoObjectSerializer>());
    }

    /// <summary>
    /// Sends a value across a silo boundary - serialized on one side, deserialized on the other.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to send.</param>
    /// <returns>The value as the receiving silo sees it.</returns>
    protected T AcrossSilos<T>(T value) => _serializer.Deserialize<T>(_serializer.SerializeToArray(value)!)!;

    /// <summary>
    /// Sends a value from a silo that has not been upgraded to one that has.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to send.</param>
    /// <returns>The value as the upgraded silo sees it.</returns>
    protected T FromAPreviousSilo<T>(T value) => _serializer.Deserialize<T>(_previousSerializer.SerializeToArray(value)!)!;

    /// <summary>
    /// Sends a value from an upgraded silo to one that has not been upgraded.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to send.</param>
    /// <returns>The value as the silo that has not been upgraded sees it.</returns>
    protected T ToAPreviousSilo<T>(T value) => _previousSerializer.Deserialize<T>(_serializer.SerializeToArray(value)!)!;

    /// <summary>
    /// Creates an <see cref="ExpandoObject"/> from name and value pairs.
    /// </summary>
    /// <param name="properties">The properties.</param>
    /// <returns>The object.</returns>
    protected static ExpandoObject Expando(params (string Name, object? Value)[] properties)
    {
        var result = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)result;
        foreach (var (name, value) in properties)
        {
            dictionary[name] = value;
        }

        return result;
    }

    /// <summary>
    /// Gets a property of an <see cref="ExpandoObject"/>.
    /// </summary>
    /// <param name="target">The object.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The value.</returns>
    protected static object? Get(object? target, string name) => ((IDictionary<string, object?>)target!)[name];

    static Serializer CreateSerializer(Action<ISerializerBuilder> registerFirst)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new JsonSerializerOptions());
        services.AddSingleton(Substitute.For<IExpandoObjectConverter>());
        services.AddSingleton(Substitute.For<IStorage>());
        services.AddSerializer(registerFirst);
        services.AddSerializer(builder => builder.Services.AddCustomSerializers());
        return services.BuildServiceProvider().GetRequiredService<Serializer>();
    }
}
