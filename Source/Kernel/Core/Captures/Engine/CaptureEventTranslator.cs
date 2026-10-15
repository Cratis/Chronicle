// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Represents an implementation of <see cref="ICaptureEventTranslator"/>.
/// </summary>
/// <param name="changeDetector"><see cref="ICaptureChangeDetector"/> for detecting what changed since the previous event for the key.</param>
/// <param name="whenClauseEvaluator"><see cref="IWhenClauseEvaluator"/> for matching changes against when clauses.</param>
/// <param name="contentMapper"><see cref="ICaptureContentMapper"/> for mapping changes to event content.</param>
[Singleton]
public class CaptureEventTranslator(
    ICaptureChangeDetector changeDetector,
    IWhenClauseEvaluator whenClauseEvaluator,
    ICaptureContentMapper contentMapper) : ICaptureEventTranslator
{
    /// <inheritdoc/>
    public CaptureEventTranslation Translate(CaptureDefinition definition, JsonObject? previous, JsonObject content, JsonObject context)
    {
        var key = ResolveKey(definition.KeyProperty, content, context)
            ?? throw new MissingKeyForCapturedEvent(
                definition.KeyProperty,
                context["eventType"]?.ToString() ?? string.Empty,
                context["sequenceNumber"]?.GetValue<ulong>() ?? 0);

        var previousState = previous is null
            ? new Dictionary<string, JsonObject>()
            : new Dictionary<string, JsonObject> { [key] = previous };
        var merged = Merge(previous, content);
        var currentState = new Dictionary<string, JsonObject> { [key] = merged };

        var events = changeDetector.Detect(previousState, currentState)
            .SelectMany(change => definition.Appends
                .Where(append => whenClauseEvaluator.Matches(append.When, change))
                .Select(append => new TranslatedCaptureEvent(append, contentMapper.Map(append, change, context))))
            .ToList();

        return new CaptureEventTranslation(key, merged, events);
    }

    static JsonObject Merge(JsonObject? previous, JsonObject content)
    {
        // Different event types carry different parts of the state - what a key looks like is what its events have said so far.
        var merged = previous?.DeepClone().AsObject() ?? [];
        foreach (var (property, value) in content)
        {
            merged[property] = value?.DeepClone();
        }

        return merged;
    }

    static string? ResolveKey(string keyProperty, JsonObject content, JsonObject context)
    {
        var node = keyProperty switch
        {
            WellKnownExpressions.EventSourceId => CaptureItemPath.Resolve(context, "eventSourceId"),
            _ when keyProperty.StartsWith("$context.", StringComparison.Ordinal) => CaptureItemPath.Resolve(context, keyProperty["$context.".Length..]),
            _ when keyProperty.StartsWith("$.", StringComparison.Ordinal) => CaptureItemPath.Resolve(content, keyProperty[2..]),
            _ => CaptureItemPath.Resolve(content, keyProperty)
        };

        var key = node?.ToString();
        return string.IsNullOrEmpty(key) ? null : key;
    }
}
