// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Represents an implementation of <see cref="ICaptureChangeDetector"/>.
/// </summary>
[Singleton]
public class CaptureChangeDetector : ICaptureChangeDetector
{
    /// <inheritdoc/>
    public IEnumerable<CaptureChange> Detect(IReadOnlyDictionary<string, JsonObject> previous, IReadOnlyDictionary<string, JsonObject> current)
    {
        var changes = new List<CaptureChange>();

        foreach (var (key, item) in current)
        {
            if (!previous.TryGetValue(key, out var previousItem))
            {
                changes.Add(new CaptureChange(key, CaptureChangeType.Added, null, item));
            }
            else if (!JsonNode.DeepEquals(previousItem, item))
            {
                changes.Add(new CaptureChange(key, CaptureChangeType.Modified, previousItem, item));
            }
        }

        changes.AddRange(previous
            .Where(kvp => !current.ContainsKey(kvp.Key))
            .Select(kvp => new CaptureChange(kvp.Key, CaptureChangeType.Removed, kvp.Value, null)));

        return changes;
    }
}
