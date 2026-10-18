// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Guards every-event payload mappings without changing event-specific mapping semantics.
/// </summary>
static class EveryEventPropertyMappers
{
    /// <summary>
    /// Wraps a payload mapper so absent source properties leave the target untouched.
    /// </summary>
    /// <param name="targetProperty">Property the mapper updates.</param>
    /// <param name="expression">Source expression.</param>
    /// <param name="mapper">Resolved mapper.</param>
    /// <returns>The guarded mapper, or the original for non-payload expressions.</returns>
    internal static PropertyMapper<AppendedEvent, ExpandoObject> WhenSourceIsPresent(
        PropertyPath targetProperty,
        string expression,
        PropertyMapper<AppendedEvent, ExpandoObject> mapper)
    {
        if (!new EventContentExpressionResolver().CanResolve(expression))
        {
            return mapper;
        }

        var sourceProperty = new PropertyPath(expression);
        if (sourceProperty.LastSegment is DerivedPropertyFunctionSegment)
        {
            sourceProperty = PropertyPath.CreateFrom(sourceProperty.Segments.SkipLast(1).ToArray());
        }

        return (@event, target, arrayIndexers) =>
            HasSource(@event.Content, sourceProperty)
                ? mapper(@event, target, arrayIndexers)
                : new(targetProperty, null, null, arrayIndexers);
    }

    static bool HasSource(ExpandoObject content, PropertyPath sourceProperty)
    {
        if (sourceProperty.IsRoot)
        {
            return true;
        }

        // HasValue on the whole path would create missing ancestors through EnsurePath.
        var parent = content.TryGetExistingPath(sourceProperty, ArrayIndexers.NoIndexers);
        var terminalProperty = PropertyPath.CreateFrom([sourceProperty.LastSegment]);
        return parent is not null && terminalProperty.HasValue(parent, ArrayIndexers.NoIndexers);
    }
}
