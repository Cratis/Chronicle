// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_migrating_an_appended_old_generation : Specification
{
    EventScenario _scenario;
    AppendResult _result;
    uint[] _generations;
    JsonElement _original;
    JsonElement _migrated;

    void Establish() => _scenario = new();

    async Task Because()
    {
        _result = await _scenario.EventLog.Append(EventSourceId.New(), new MigrationTopicCreatedV1());

        // Inspect stored generations, not default read delivery, which is owned by the storage provider.
        var root = _scenario.TestingStore;
        var store = root.GetType().GetProperty("Store", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(root)!;
        var ns = Call(store, "GetNamespace", root.Namespace.Value);
        var sequence = Call(ns, "GetEventSequence", EventSequenceId.Log.Value);
        var task = (Task)Call(sequence, "GetStoredGenerations", EventSequenceNumber.First.Value);
        await task;
        var stored = Value(task, "Result");
        var content = (IDictionary)Value(stored, "Content");
        var keys = content.Keys.Cast<object>().ToArray();
        _generations = keys.Select(key => (uint)Value(key, "Value")).Order().ToArray();
        _original = JsonSerializer.Deserialize<JsonElement>((string)content[keys.Single(key => (uint)Value(key, "Value") == 1)]!);
        _migrated = JsonSerializer.Deserialize<JsonElement>((string)content[keys.Single(key => (uint)Value(key, "Value") == 2)]!);
    }

    void Destroy() => _scenario.Dispose();

    static object Value(object instance, string name) => instance.GetType().GetProperty(name)!.GetValue(instance)!;

    static object Call(object instance, string name, object value)
    {
        // Kernel concept types are private package dependencies, not consumer API.
        var method = instance.GetType().GetMethod(name)!;
        var argument = Activator.CreateInstance(method.GetParameters()[0].ParameterType, value);
        var arguments = new[] { argument }.Concat(method.GetParameters().Skip(1).Select(_ => Type.Missing)).ToArray();
        return method.Invoke(instance, arguments)!;
    }

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_store_the_appended_and_migrated_generations() => _generations.ShouldContainOnly(1U, 2U);
    [Fact] void should_store_the_migrated_value_in_generation_two() => _migrated.GetProperty("module").GetString().ShouldEqual("global");
    [Fact] void should_preserve_the_appended_generations_original_content() => _original.EnumerateObject().ShouldBeEmpty();
}
