// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Represents an implementation of <see cref="IWhenClauseEvaluator"/>.
/// </summary>
[Singleton]
public class WhenClauseEvaluator : IWhenClauseEvaluator
{
    /// <inheritdoc/>
    public bool Matches(WhenClause when, CaptureChange change) => when.Type switch
    {
        WhenClauseType.Added => change.Type == CaptureChangeType.Added,
        WhenClauseType.Removed => change.Type == CaptureChangeType.Removed,
        WhenClauseType.PropertyChange => change.Type == CaptureChangeType.Modified && when.Properties.Any(property => PropertyChanged(change, property)),
        WhenClauseType.LogicalOr => change.Type == CaptureChangeType.Modified && when.Properties.Any(property => PropertyChanged(change, property)),
        WhenClauseType.LogicalAnd => change.Type == CaptureChangeType.Modified && when.Properties.All(property => PropertyChanged(change, property)),
        WhenClauseType.ValueTransition => MatchesTransition(when, change),
        WhenClauseType.Expression => throw new UnsupportedCaptureCapability("Expression based when clauses are not supported by the capturing engine yet"),
        _ => false
    };

    static bool PropertyChanged(CaptureChange change, string property)
    {
        var previousValue = CaptureItemPath.Resolve(change.Previous, property);
        var currentValue = CaptureItemPath.Resolve(change.Current, property);
        return !JsonNode.DeepEquals(previousValue, currentValue);
    }

    static bool MatchesTransition(WhenClause when, CaptureChange change)
    {
        if (change.Type != CaptureChangeType.Modified || when.Properties.Count == 0)
        {
            return false;
        }

        var property = when.Properties[0];
        var previousValue = CaptureItemPath.Resolve(change.Previous, property)?.ToString();
        var currentValue = CaptureItemPath.Resolve(change.Current, property)?.ToString();
        return previousValue == when.FromValue && currentValue == when.ToValue;
    }
}
