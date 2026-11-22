// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Setup.Serialization.for_KernelSerializers.given;

/// <summary>
/// Orleans serialization configured the way the kernel silo configures it.
/// </summary>
public class the_kernel_serializers : Specification
{
    protected ServiceProvider _provider;
    protected Serializer _serializer;
    protected DeepCopier _copier;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IExpandoObjectConverter>(new ExpandoObjectConverter(new TypeFormats()));
        services.AddSingleton(StorageWithAnEventTypeSchema());
        SerializationConfigurationExtensions.ConfigureKernelSerialization(services);
        _provider = services.BuildServiceProvider();
        _serializer = _provider.GetRequiredService<Serializer>();
        _copier = _provider.GetRequiredService<DeepCopier>();
    }

    void Destroy() => _provider.Dispose();

    static IStorage StorageWithAnEventTypeSchema()
    {
        var schema = new EventTypeSchema(
            EventType.Unknown,
            EventTypeOwner.Client,
            EventTypeSource.Code,
            JsonSchema.FromJson("{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"},\"count\":{\"type\":\"integer\"}}}"));
        var eventTypes = Substitute.For<IEventTypesStorage>();
        eventTypes.GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(_ => Task.FromResult(schema));
        var eventStore = Substitute.For<IEventStoreStorage>();
        eventStore.EventTypes.Returns(eventTypes);
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        return storage;
    }

    /// <summary>
    /// Send a value through the serializer, the way a call between two silos does.
    /// </summary>
    /// <param name="type">The type of the value.</param>
    /// <param name="value">The value to send.</param>
    /// <returns>What the receiving silo gets.</returns>
    protected object? SendBetweenSilos(Type type, object value)
    {
        var typed = typeof(Serializer).GetMethods().First(_ => _.Name == nameof(Serializer.SerializeToArray) && _.IsGenericMethodDefinition && _.GetParameters().Length == 1).MakeGenericMethod(type);
        var bytes = (byte[])typed.Invoke(_serializer, [value])!;
        var deserialize = typeof(Serializer).GetMethods().First(_ => _.Name == nameof(Serializer.Deserialize) && _.IsGenericMethodDefinition && _.GetParameters().Length == 1 && _.GetParameters()[0].ParameterType == typeof(byte[])).MakeGenericMethod(type);
        return deserialize.Invoke(_serializer, [bytes]);
    }

    /// <summary>
    /// Pass a value through the copier, the way a call within one silo does.
    /// </summary>
    /// <param name="type">The type of the value.</param>
    /// <param name="value">The value to pass.</param>
    /// <returns>What the receiving grain gets.</returns>
    protected object? PassWithinSilo(Type type, object value)
    {
        var copy = typeof(DeepCopier).GetMethods().First(_ => _.Name == nameof(DeepCopier.Copy) && _.IsGenericMethodDefinition && _.GetParameters().Length == 1).MakeGenericMethod(type);
        return copy.Invoke(_copier, [value]);
    }
}
