// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Types;
using CorrelationId = Cratis.Execution.CorrelationId;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>Measures prepared-path accumulation, removal and set-once with unchanged operation baselines.</summary>
[MemoryDiagnoser]
[BenchmarkCategory("PropertyMappersOperations")]
public class PropertyMappersOperationsBenchmark
{
    const string Key = "slot";
    static readonly PropertyPath _path = "values.$eventContent.slot";
    PropertyMapper<AppendedEvent, ExpandoObject> _baseline = null!;
    PropertyMapper<AppendedEvent, ExpandoObject> _current = null!;
    AppendedEvent _event = null!;
    ExpandoObject _baselineTarget = null!;
    ExpandoObject _currentTarget = null!;
    IDictionary<string, object?> _baselineValues = null!;
    IDictionary<string, object?> _currentValues = null!;
    Action<IDictionary<string, object?>> _reset = null!;

    /// <summary>Gets or sets the operation, including the already-set and first-write set-once controls.</summary>
    [Params("add", "remove", "set-once", "set-once-empty")]
    public string Operation { get; set; } = "add";

    /// <summary>Prepares independent targets, reusable mappers and identical target-restoration strategies.</summary>
    /// <exception cref="InvalidOperationException">The benchmark schema does not resolve to the expected integer type.</exception>
    [GlobalSetup]
    public void Setup()
    {
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)[Key] = Key;
        _event = new(new(new("event", 1), EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, 0, DateTimeOffset.UnixEpoch, "store", "namespace", new CorrelationId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")), [], Identity.System, [], EventHash.NotSet), content);
        var typeFormats = new TypeFormats();
        var schema = new JsonSchemaProperty { Type = JsonObjectType.Integer, Format = "int32" };
        var targetType = schema.GetTargetTypeForJsonSchemaProperty(typeFormats) ?? typeof(double);
        if (targetType != typeof(int))
        {
            throw new InvalidOperationException("The operation benchmark requires an int32 target.");
        }

        static object ProvideValue(AppendedEvent appendedEvent) => 1;
        _baseline = BaselineMapper(targetType, ProvideValue);
        _current = CurrentMapper(typeFormats, schema, ProvideValue);
        _reset = ResetStrategy();
        _baselineTarget = CreateTarget(out _baselineValues);
        _currentTarget = CreateTarget(out _currentValues);
        VerifyEquivalent(MapBaseline(), MapCurrent(), Operation != "set-once");
    }

    /// <summary>Runs the unchanged operation and restores its target for the next invocation.</summary>
    /// <returns>The complete mapped difference.</returns>
    [Benchmark(Baseline = true)]
    public PropertyDifference MapBaseline()
    {
        var result = _baseline(_event, _baselineTarget, ArrayIndexers.NoIndexers);
        _reset(_baselineValues);
        return result;
    }

    /// <summary>Runs the production operation with the same target-restoration work.</summary>
    /// <returns>The complete mapped difference.</returns>
    [Benchmark]
    public PropertyDifference MapCurrent()
    {
        var result = _current(_event, _currentTarget, ArrayIndexers.NoIndexers);
        _reset(_currentValues);
        return result;
    }

    static void VerifyEquivalent(PropertyDifference baseline, PropertyDifference current, bool shouldChange)
    {
        var before = (baseline.PropertyPath.Path, baseline.Original, baseline.Changed, baseline.IsRemoval, baseline.ArrayIndexers);
        var after = (current.PropertyPath.Path, current.Original, current.Changed, current.IsRemoval, current.ArrayIndexers);
        if (!before.Equals(after))
        {
            throw new InvalidOperationException("The operation benchmark differences disagree.");
        }

        if (current.HasChanges() != shouldChange)
        {
            throw new InvalidOperationException("The operation benchmark unexpectedly became a no-op or changed its set-once control.");
        }
    }

    static PropertyMapper<AppendedEvent, ExpandoObject> BaselineAdd(Type targetType, ValueProvider<AppendedEvent> provider)
    {
        var operations = NumericOperationsFor(CoerceTo<int>);
        return BaselineAccumulate(targetType, (current, @event) => operations.Add(current, provider(@event)));
    }

    static PropertyMapper<AppendedEvent, ExpandoObject> BaselineAccumulate(Type targetType, Func<object, AppendedEvent, object> accumulate)
    {
        return (@event, target, arrayIndexers) =>
        {
            if (PropertyMappersBenchmark.ResolveDynamicPropertyPath(_path, @event) is not { } resolvedProperty)
            {
                return NoChange(_path, arrayIndexers);
            }

            var lastSegment = resolvedProperty.LastSegment;
            var actualTarget = target.EnsurePath(resolvedProperty, arrayIndexers) as IDictionary<string, object>;
            if (!actualTarget.TryGetValue(lastSegment.Value, out var valueAsObject))
            {
                valueAsObject = TypeConversion.Convert(targetType, 0);
                actualTarget[lastSegment.Value] = valueAsObject;
            }

            var result = accumulate(valueAsObject, @event);
            actualTarget[lastSegment.Value] = result;
            return new(resolvedProperty, valueAsObject, result, arrayIndexers);
        };
    }

    static PropertyMapper<AppendedEvent, ExpandoObject> BaselineSetOnce(ValueProvider<AppendedEvent> eventValueProvider)
    {
        return (@event, target, arrayIndexers) =>
        {
            if (PropertyMappersBenchmark.ResolveDynamicPropertyPath(_path, @event) is not { } resolvedProperty)
            {
                return NoChange(_path, arrayIndexers);
            }

            var existingParent = target.TryGetExistingPath(resolvedProperty, arrayIndexers) as IDictionary<string, object?>;
            if (existingParent?.TryGetValue(resolvedProperty.LastSegment.Value, out var existing) == true && existing is not null)
            {
                return NoChange(resolvedProperty, arrayIndexers);
            }

            var actualTarget = target.EnsurePath(resolvedProperty, arrayIndexers) as IDictionary<string, object>;
            var property = resolvedProperty.LastSegment.Value;
            var originalValue = actualTarget.TryGetValue(property, out var value) ? value : null;
            var newValue = actualTarget[property] = StateValues.Copy(eventValueProvider(@event));
            return new(resolvedProperty, originalValue, newValue, arrayIndexers);
        };
    }

    static PropertyMapper<AppendedEvent, ExpandoObject> BaselineRemove()
    {
        return (@event, target, arrayIndexers) =>
        {
            if (PropertyMappersBenchmark.ResolveDynamicPropertyPath(_path, @event) is not { } resolvedProperty)
            {
                return NoChange(_path, arrayIndexers);
            }

            return TryRemoveKey(target, resolvedProperty, arrayIndexers, out var original)
                ? PropertyDifference.Removal(resolvedProperty, original, arrayIndexers)
                : NoChange(_path, arrayIndexers);
        };
    }

    static bool TryRemoveKey(ExpandoObject target, PropertyPath path, ArrayIndexers arrayIndexers, out object? original)
    {
        original = null;
        return target.TryGetExistingPath(path, arrayIndexers) is IDictionary<string, object?> parent &&
            parent.Remove(path.LastSegment.Value, out original);
    }

    static PropertyDifference NoChange(PropertyPath propertyPath, ArrayIndexers arrayIndexers) => new(propertyPath, null, null, arrayIndexers);

    static NumericOperations NumericOperationsFor<T>(Func<object, T> coerce)
        where T : INumber<T> => new(
            (current, operand) => coerce(current) + coerce(operand),
            (current, operand) => coerce(current) - coerce(operand));

    static T CoerceTo<T>(object value) => (T)Convert.ChangeType(value, typeof(T));

    ExpandoObject CreateTarget(out IDictionary<string, object?> values)
    {
        var target = new ExpandoObject();
        var dictionary = new ExpandoObject();
        ((IDictionary<string, object?>)target)["values"] = dictionary;
        values = dictionary;
        if (Operation != "set-once-empty")
        {
            values[Key] = Operation == "add" ? 0 : 1;
        }

        return target;
    }

    PropertyMapper<AppendedEvent, ExpandoObject> BaselineMapper(Type targetType, ValueProvider<AppendedEvent> provider) => Operation switch
    {
        "add" => BaselineAdd(targetType, provider),
        "remove" => BaselineRemove(),
        _ => BaselineSetOnce(provider)
    };

    PropertyMapper<AppendedEvent, ExpandoObject> CurrentMapper(ITypeFormats typeFormats, JsonSchemaProperty schema, ValueProvider<AppendedEvent> provider) => Operation switch
    {
        "add" => PropertyMappers.AddWithEventValueProvider(typeFormats, _path, schema, provider),
        "remove" => PropertyMappers.Remove(_path),
        _ => PropertyMappers.SetOnceFromEventValueProvider(_path, provider)
    };

    Action<IDictionary<string, object?>> ResetStrategy() => Operation switch
    {
        "add" => values => values[Key] = 0,
        "remove" => values => values[Key] = 1,
        "set-once-empty" => values => values.Remove(Key),
        _ => _ => { }
    };
}
