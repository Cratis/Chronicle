// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using Cratis.Chronicle.Dynamic;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Copies values before they enter the state of one read model instance.
/// </summary>
/// <remarks>
/// The initial model state of a projection and the content of an appended event are shared - every partition of a
/// projection, and every observer of an event, sees the same instances. A complex value taken from either must not
/// become part of one instance's state or changes as-is: the pipeline and the sink write into that state in place,
/// and an <see cref="ExpandoObject"/> being written to while another partition enumerates it fails with
/// "Collection was modified".
/// </remarks>
internal static class StateValues
{
    const string ValueKey = "value";

    /// <summary>
    /// Get a copy of a value that shares no mutable instance with the original.
    /// </summary>
    /// <param name="value">Value to copy.</param>
    /// <returns>The value itself when it is immutable, otherwise a deep copy.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static object? Copy(object? value) => value switch
    {
        null => null,
        string => value,
        ExpandoObject expandoObject => expandoObject.Clone(),
        IEnumerable => CopyThroughHolder(value),
        _ => value
    };

    static object? CopyThroughHolder(object value)
    {
        var holder = new ExpandoObject();
        ((IDictionary<string, object?>)holder)[ValueKey] = value;
        return ((IDictionary<string, object?>)holder.Clone())[ValueKey];
    }
}
