// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelEvents;

public class when_preparing_accumulated_input_again : Specification
{
    EventStoreForTesting _store;
    EventSourceId _source;
    ulong _count;
    uint? _appendedGeneration;
    uint[] _generations;
    string _migratedContent;

    async Task Establish()
    {
        _store = new();
        _source = EventSourceId.New();
        await Prepare([(_source, new MigrationTopicCreatedV1())]);
    }

    async Task Because()
    {
        var storage = await Prepare([(_source, new MigrationTopicCreatedV1()), (_source, new MigrationTopicCreated("second"))]);
        var count = await Invoke(storage.GetType().GetMethod("GetCount")!, storage, []);
        _count = (ulong)Value(count!, "Value")!;
        var read = storage.GetType().GetMethod("GetStoredGenerations")!;
        var position = Activator.CreateInstance(read.GetParameters()[0].ParameterType, 0UL);
        var stored = (await Invoke(read, storage, [position]))!;
        _appendedGeneration = (uint?)Value(Value(stored, "AppendedGeneration")!, "Value");
        var content = (IDictionary)Value(stored, "Content")!;
        _generations = content.Keys.Cast<object>().Select(key => (uint)Value(key, "Value")!).Order().ToArray();
        var migratedGeneration = content.Keys.Cast<object>().Single(key => (uint)Value(key, "Value")! == 2);
        _migratedContent = (string)content[migratedGeneration]!;
    }

    void Destroy() => _store.Dispose();

    async Task<object> Prepare(IReadOnlyList<(EventSourceId EventSourceId, object Event)> events)
    {
        // Kernel types are private dependencies; keep the raw-generation regression at the internal Testing boundary.
        var method = typeof(ReadModelEvents).GetMethod("Prepare", BindingFlags.Static | BindingFlags.NonPublic)!;
        var prepared = (await Invoke(method, null, [_store, events, null, true]))!;
        return prepared.GetType().GetField("Item2")!.GetValue(prepared)!;
    }

    static object? Value(object instance, string name) => instance.GetType().GetProperty(name)!.GetValue(instance);

    static async Task<object?> Invoke(MethodInfo method, object? instance, object?[] arguments)
    {
        var supplied = arguments.Concat(method.GetParameters().Skip(arguments.Length).Select(_ => Type.Missing)).ToArray();
        var task = (Task)method.Invoke(instance, supplied)!;
        await task;
        return Value(task, "Result");
    }

    [Fact] void should_append_only_the_new_suffix() => _count.ShouldEqual(2UL);
    [Fact] void should_preserve_the_appended_generation() => _appendedGeneration.ShouldEqual(1U);
    [Fact] void should_preserve_both_generation_payloads() => _generations.ShouldContainOnly(1U, 2U);
    [Fact] void should_store_the_migrated_value() => _migratedContent.ShouldContain("global");
}
