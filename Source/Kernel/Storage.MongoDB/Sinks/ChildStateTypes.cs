// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Classifies the state of an added or removed child.
/// </summary>
internal static class ChildStateTypes
{
    /// <summary>
    /// Gets whether a child state is a primitive value rather than a document.
    /// </summary>
    /// <param name="state">The state of the child.</param>
    /// <returns>True when the state is a primitive, false when it is a document.</returns>
    public static bool IsPrimitive(object state)
    {
        var stateType = state.GetType();
        return stateType.IsPrimitive || stateType == typeof(string) || stateType == typeof(Guid) || stateType == typeof(DateTime) || stateType == typeof(DateTimeOffset) || stateType == typeof(DateOnly) || stateType == typeof(TimeOnly);
    }
}
