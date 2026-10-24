// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_reading_instances;

/// <summary>
/// A read model keyed by a composite key returns the key object when its instances are read, whichever way they are read.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISinkHarness"/> supplying the implementation under specification.</typeparam>
public abstract class and_the_key_is_composite<THarness> : given.an_accumulating_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    const string First = "6f1c2c43-6c8c-4b3c-9d1c-3a7b4f0e2a11";
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(30);

    object? _pageKey;
    object? _keyedKey;
    object? _observedKey;

    async Task Establish()
    {
        _key = CompositeKey();
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 1);
    }

    async Task Because()
    {
        var page = await _sink.GetInstances();
        _pageKey = KeyOf(page.Instances.Single());

        var keyed = await _sink.FindInPrimaryOrDefault(_key);
        _keyedKey = KeyOf(keyed!);

        var observed = new TaskCompletionSource<ExpandoObject>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = _sink.ObserveInstances().Subscribe(
            instances =>
            {
                var current = instances.ToArray();
                if (current.Length == 1)
                {
                    observed.TrySetResult(current[0]);
                }
            },
            error => observed.TrySetException(error));
        _observedKey = KeyOf(await observed.Task.WaitAsync(_deadline));
    }

    [Fact] public void should_return_the_key_from_a_page() => ShouldBeTheKey(_pageKey);
    [Fact] public void should_return_the_key_from_a_keyed_read() => ShouldBeTheKey(_keyedKey);
    [Fact] public void should_return_the_key_from_an_observed_page() => ShouldBeTheKey(_observedKey);

    protected override ReadModelDefinition CreateReadModelDefinition() => base.CreateReadModelDefinition() with
    {
        Schemas = new Dictionary<ReadModelGeneration, JsonSchema>
        {
            [ReadModelGeneration.First] = JsonSchema.FromJson("""
                {"type":"object","properties":{"id":{"type":"object","properties":{"first":{"type":"string"},"second":{"type":"integer","format":"int32"}}},"count":{"type":"integer"}}}
                """)
        }
    };

    static Key CompositeKey()
    {
        var value = new ExpandoObject();
        ((IDictionary<string, object?>)value)["first"] = First;
        ((IDictionary<string, object?>)value)["second"] = 42;
        return new Key(value, ArrayIndexers.NoIndexers);
    }

    static object? KeyOf(ExpandoObject instance) => ((IDictionary<string, object?>)instance).TryGetValue("id", out var key) ? key : null;

    static void ShouldBeTheKey(object? key)
    {
        var parts = (key as IDictionary<string, object?>)!;
        parts.ShouldNotBeNull();
        Convert.ToString(parts["first"], CultureInfo.InvariantCulture).ShouldEqual(First);
        Convert.ToString(parts["second"], CultureInfo.InvariantCulture).ShouldEqual("42");
    }
}
