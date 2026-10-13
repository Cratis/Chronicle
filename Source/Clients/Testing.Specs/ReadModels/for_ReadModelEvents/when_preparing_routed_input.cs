// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelEvents;

public class when_preparing_routed_input : Specification
{
    EventStoreForTesting _store;
    EventSourceId _source;
    string _storedSourceType;
    string _storedStreamType;
    string _storedStreamId;
    ulong _storedCount;
    string[] _storedTags;
    string[] _deliveredTags;

    void Establish()
    {
        _store = new();
        _source = EventSourceId.New();
    }

    async Task Because()
    {
        IReadOnlyList<ReadModelEvent> seeds = [new(_source, new RoutedEventRecorded("stored"), "Order", "Payments", "2026-10", ["stored-tag", "append-tag"])];
        IReadOnlyList<(EventSourceId EventSourceId, object Event)> events = seeds.Select(seed => (seed.EventSourceId, seed.Event)).ToArray();

        // Kernel types are private dependencies; inspect persisted routing at the internal Testing boundary.
        var prepare = typeof(ReadModelEvents).GetMethod("Prepare", BindingFlags.Static | BindingFlags.NonPublic)!;
        var prepared = (await Invoke(prepare, null, [_store, events, null, true, seeds]))!;
        var storage = prepared.GetType().GetField("Item2")!.GetValue(prepared)!;
        var read = storage.GetType().GetMethod("GetEventAt")!;
        var position = Activator.CreateInstance(read.GetParameters()[0].ParameterType, 0UL);
        var stored = (await Invoke(read, storage, [position]))!;
        var context = Value(stored, "Context")!;
        _storedSourceType = (string)Value(Value(context, "EventSourceType")!, "Value")!;
        _storedStreamType = (string)Value(Value(context, "EventStreamType")!, "Value")!;
        _storedStreamId = (string)Value(Value(context, "EventStreamId")!, "Value")!;
        _storedTags = Tags(context);
        var delivered = ((Array)prepared.GetType().GetField("Item1")!.GetValue(prepared)!).GetValue(0)!;
        _deliveredTags = Tags(Value(delivered, "Context")!);
        var count = (await Invoke(storage.GetType().GetMethod("GetCount")!, storage, []))!;
        _storedCount = (ulong)Value(count, "Value")!;
    }

    void Destroy() => _store.Dispose();

    static string[] Tags(object context) => ((IEnumerable)Value(context, "Tags")!).Cast<object>().Select(tag => (string)Value(tag, "Value")!).ToArray();

    static object? Value(object instance, string name) => instance.GetType().GetProperty(name)!.GetValue(instance);

    static async Task<object?> Invoke(MethodInfo method, object? instance, object?[] arguments)
    {
        var supplied = arguments.Concat(method.GetParameters().Skip(arguments.Length).Select(_ => Type.Missing)).ToArray();
        var task = (Task)method.Invoke(instance, supplied)!;
        await task;
        return Value(task, "Result");
    }

    [Fact] void should_persist_the_source_type() => _storedSourceType.ShouldEqual("Order");
    [Fact] void should_persist_the_stream_type() => _storedStreamType.ShouldEqual("Payments");
    [Fact] void should_persist_the_stream_id() => _storedStreamId.ShouldEqual("2026-10");
    [Fact] void should_store_the_seed() => _storedCount.ShouldEqual(1UL);
    [Fact] void should_persist_effective_seed_tags() => _storedTags.ShouldContainOnly("stored-tag", "append-tag");
    [Fact] void should_deliver_the_persisted_tags() => _deliveredTags.ShouldContainOnly("stored-tag", "append-tag");
}
