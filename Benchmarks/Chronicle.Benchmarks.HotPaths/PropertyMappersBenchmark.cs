// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using Cratis.Concepts;
using CorrelationId = Cratis.Execution.CorrelationId;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>Measures invocation, not creation, of reusable static and dynamic property mappers.</summary>
[MemoryDiagnoser]
[BenchmarkCategory("PropertyMappers")]
public class PropertyMappersBenchmark
{
    PropertyMapper<AppendedEvent, ExpandoObject> _baseline = null!;
    PropertyMapper<AppendedEvent, ExpandoObject> _current = null!;
    AppendedEvent _event = null!;
    ExpandoObject _baselineTarget = null!;
    ExpandoObject _currentTarget = null!;

    /// <summary>Gets or sets the path shape.</summary>
    [Params("static", "nested", "content", "context", "cause")]
    public string PathKind { get; set; } = "static";

    /// <summary>Constructs stable events, mappers and independent overwrite targets.</summary>
    /// <exception cref="InvalidOperationException">The baseline and production paths differ.</exception>
    [GlobalSetup]
    public void Setup()
    {
        PropertyPath path = PathKind switch
        {
            "nested" => "outer.middle.value",
            "content" => "values.$eventContent.slot",
            "context" => "values.$eventContext.eventType.id",
            "cause" => "values.$causedBy.subject",
            _ => "value"
        };
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["slot"] = "slot";
        _event = new(new(new("event", 1), EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, 0, DateTimeOffset.UnixEpoch, "store", "namespace", new CorrelationId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")), [], Identity.System, [], EventHash.NotSet), content);
        _baseline = BaselineMapper(path, _ => "value");
        _current = PropertyMappers.FromEventValueProvider(path, _ => "value");
        _baselineTarget = new();
        _currentTarget = new();
        if (MapBaseline().PropertyPath != MapCurrent().PropertyPath)
        {
            throw new InvalidOperationException("Mapper benchmark paths disagree.");
        }
    }

    /// <summary>Invokes the unchanged mapper from 81fb14c.</summary>
    /// <returns>The complete difference.</returns>
    [Benchmark(Baseline = true)]
    public PropertyDifference MapBaseline() => _baseline(_event, _baselineTarget, ArrayIndexers.NoIndexers);

    /// <summary>Invokes the production mapper.</summary>
    /// <returns>The complete difference.</returns>
    [Benchmark]
    public PropertyDifference MapCurrent() => _current(_event, _currentTarget, ArrayIndexers.NoIndexers);

    /// <summary>
    /// Runs the baseline algorithm from 81fb14c with two extracted, inlined validation predicates.
    /// Parsing, key validation, property-path construction and provider lookup still run on every invocation.
    /// </summary>
    /// <param name="targetProperty">The unresolved target property path.</param>
    /// <param name="eventValueProvider">The provider invoked for every mapped event.</param>
    /// <returns>The reusable baseline mapper.</returns>
    static PropertyMapper<AppendedEvent, ExpandoObject> BaselineMapper(PropertyPath targetProperty, ValueProvider<AppendedEvent> eventValueProvider)
    {
        return (@event, target, arrayIndexers) =>
        {
            if (ResolveDynamicPropertyPath(targetProperty, @event) is not { } resolvedProperty)
            {
                return new(targetProperty, null, null, arrayIndexers);
            }

            var actualTarget = target.EnsurePath(resolvedProperty, arrayIndexers) as IDictionary<string, object>;
            var property = resolvedProperty.LastSegment.Value;
            var originalValue = actualTarget.TryGetValue(property, out var value) ? value : null;
            var newValue = actualTarget[property] = StateValues.Copy(eventValueProvider(@event));
            return new(resolvedProperty, originalValue, newValue, arrayIndexers);
        };
    }

    static PropertyPath? ResolveDynamicPropertyPath(PropertyPath propertyPath, AppendedEvent @event)
    {
        var pathString = propertyPath.Path;
        var markerIndex = pathString.IndexOf(".$", StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return propertyPath;
        }

        var staticPrefix = pathString[..markerIndex];
        var expressionStart = markerIndex + 1;
        var propertyStart = pathString.IndexOf('.', expressionStart);
        if (propertyStart < 0 || propertyStart == pathString.Length - 1)
        {
            throw new UnsupportedDynamicPropertyPathExpression(propertyPath);
        }

        var expression = pathString[expressionStart..propertyStart];
        var property = pathString[(propertyStart + 1)..];
        if (expression == WellKnownExpressions.EventContent)
        {
            var key = ReadExistingEventContentValue(@event.Content, new PropertyPath(property));
            return key is null ? null : new PropertyPath($"{staticPrefix}.{EventContentDictionaryKeyFor(propertyPath, key)}");
        }

        var contextPath = expression switch
        {
            WellKnownExpressions.EventContext => property,
            WellKnownExpressions.CausedBy when IsCausingIdentityProperty(property) => $"causedBy.{property}",
            _ => throw new UnsupportedDynamicPropertyPathExpression(propertyPath)
        };
        var resolvedValue = EventValueProviders.EventContext(new PropertyPath(contextPath))(@event);
        return resolvedValue is null ? null : new PropertyPath($"{staticPrefix}.{FormatEventContextKeyValue(resolvedValue)}");
    }

    static string FormatEventContextKeyValue(object value) => value switch
    {
        _ when value.IsConcept() => value.GetConceptValue() is { } conceptValue ? FormatEventContextKeyValue(conceptValue) : string.Empty,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    static string EventContentDictionaryKeyFor(PropertyPath propertyPath, object? value)
    {
        var key = FormatDynamicKeyValue(value);
        if (IsInvalidDictionaryKey(key))
        {
            throw new InvalidDictionaryKey(propertyPath, key);
        }

        return key;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IsInvalidDictionaryKey(string key) => key.Length == 0 || key.AsSpan().IndexOfAny(".[]\0") >= 0 || key.StartsWith('$');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IsCausingIdentityProperty(string property) => property.Equals("subject", StringComparison.Ordinal) || property.Equals("name", StringComparison.Ordinal) || property.Equals("userName", StringComparison.Ordinal);

    static object? ReadExistingEventContentValue(ExpandoObject content, PropertyPath path)
    {
        var parent = content.TryGetExistingPath(path, ArrayIndexers.NoIndexers) as IDictionary<string, object?>;
        return parent?.TryGetValue(path.LastSegment.Value, out var value) == true ? value : null;
    }

    static string FormatDynamicKeyValue(object? value) => value switch
    {
        null => string.Empty,
        _ when value.IsConcept() => FormatDynamicKeyValue(value.GetConceptValue()),
        DateTimeOffset or DateTime or DateOnly or TimeOnly => FormatTemporalKeyValue(value),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    static string FormatTemporalKeyValue(object value) => value switch
    {
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss','fffffffK", CultureInfo.InvariantCulture),
        DateTime dateTime => dateTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss','fffffffK", CultureInfo.InvariantCulture),
        DateOnly dateOnly => dateOnly.ToString("O", CultureInfo.InvariantCulture),
        _ => ((TimeOnly)value).ToString("HH':'mm':'ss','fffffff", CultureInfo.InvariantCulture)
    };
}
