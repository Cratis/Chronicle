// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Represents an implementation of <see cref="ICaptureContentMapper"/>.
/// </summary>
[Singleton]
public class CaptureContentMapper : ICaptureContentMapper
{
    /// <inheritdoc/>
    public JsonObject Map(AppendDefinition append, CaptureChange change)
    {
        var item = change.Current ?? change.Previous ?? [];

        if (append.FieldAssignments.Count == 0)
        {
            return item.DeepClone().AsObject();
        }

        var content = new JsonObject();
        foreach (var (property, expression) in append.FieldAssignments)
        {
            content[property] = ResolveExpression(expression, item);
        }

        return content;
    }

    static JsonNode? ResolveExpression(string expression, JsonObject item)
    {
        if (expression.StartsWith("$.", StringComparison.Ordinal))
        {
            return CaptureItemPath.Resolve(item, expression[2..])?.DeepClone();
        }

        if (expression.StartsWith('$') || expression.StartsWith('`'))
        {
            throw new UnsupportedCaptureCapability($"The expression '{expression}' is not supported by the capturing engine yet");
        }

        if (expression.Length >= 2 && expression.StartsWith('"') && expression.EndsWith('"'))
        {
            return JsonValue.Create(expression[1..^1]);
        }

        if (bool.TryParse(expression, out var boolean))
        {
            return JsonValue.Create(boolean);
        }

        if (long.TryParse(expression, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
        {
            return JsonValue.Create(integer);
        }

        if (double.TryParse(expression, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return JsonValue.Create(number);
        }

        return CaptureItemPath.Resolve(item, expression)?.DeepClone();
    }
}
