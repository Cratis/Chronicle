// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Represents a set of values associated with a <see cref="ReactorContext"/>.
/// </summary>
public class ReactorContextValues : Dictionary<string, object>
{
    /// <summary>
    /// Gets an empty set of reactor context values.
    /// </summary>
    public static readonly ReactorContextValues Empty = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactorContextValues"/> class.
    /// </summary>
    public ReactorContextValues()
        : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Merges another set of reactor context values into this instance.
    /// </summary>
    /// <param name="other">The other set of reactor context values to merge.</param>
    public void Merge(ReactorContextValues other)
    {
        foreach (var kvp in other)
        {
            this[kvp.Key] = kvp.Value;
        }
    }
}
