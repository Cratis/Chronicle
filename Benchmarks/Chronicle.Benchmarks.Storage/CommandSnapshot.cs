// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>The commands a <see cref="CommandCounter"/> has seen.</summary>
/// <param name="Total">The number of commands that reached the database.</param>
/// <param name="ByType">The number of commands per command type (a MongoDB command name or a SQL verb).</param>
public sealed record CommandSnapshot(long Total, IReadOnlyDictionary<string, long> ByType)
{
    /// <summary>Gets the commands seen after another snapshot was taken.</summary>
    /// <param name="earlier">The earlier snapshot.</param>
    /// <returns>The difference.</returns>
    public CommandSnapshot Since(CommandSnapshot earlier)
    {
        var byType = new Dictionary<string, long>();
        foreach (var (type, count) in ByType)
        {
            var difference = count - (earlier.ByType.TryGetValue(type, out var before) ? before : 0);
            if (difference != 0) byType[type] = difference;
        }

        return new(Total - earlier.Total, byType);
    }
}
