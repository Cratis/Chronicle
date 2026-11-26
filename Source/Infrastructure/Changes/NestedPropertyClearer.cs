// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes;

/// <summary>
/// Clears a nested property on a state in place.
/// </summary>
internal static class NestedPropertyClearer
{
    /// <summary>
    /// Set a nested property to null on a state, when the state holds it.
    /// </summary>
    /// <param name="state">The state to clear the property on.</param>
    /// <param name="nestedProperty">The nested property to clear.</param>
    /// <param name="arrayIndexers">Indexers selecting elements of the arrays on the path.</param>
    internal static void Clear(object? state, PropertyPath nestedProperty, ArrayIndexers arrayIndexers)
    {
        if (state is ExpandoObject expando)
        {
            ClearInExpando(expando, nestedProperty, arrayIndexers);
            return;
        }

        ClearInObject(state, nestedProperty, arrayIndexers);
    }

    static void ClearInExpando(ExpandoObject expando, PropertyPath nestedProperty, ArrayIndexers arrayIndexers)
    {
        if (expando.TryGetExistingPath(nestedProperty, arrayIndexers) is { } parent)
        {
            ((IDictionary<string, object?>)parent)[nestedProperty.LastSegment.Value] = null;
        }
    }

    static void ClearInObject(object? state, PropertyPath nestedProperty, ArrayIndexers arrayIndexers)
    {
        var parentPath = PropertyPath.CreateFrom([.. nestedProperty.Segments.SkipLast(1)]);
        if (TryGetParent(state, parentPath, arrayIndexers, out var parent))
        {
            PropertyPath.CreateFrom([nestedProperty.LastSegment]).GetPropertyInfoFor(parent.GetType()).SetValue(parent, null);
        }
    }

    static bool TryGetParent(object? state, PropertyPath parentPath, ArrayIndexers arrayIndexers, [NotNullWhen(true)] out object? parent)
    {
        parent = state;
        return (parentPath.IsRoot || ChangesetStateNavigator.TryGetValueAtPath(state, parentPath, arrayIndexers, out parent)) && parent is not null;
    }
}
