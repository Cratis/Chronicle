// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Decides which types the silo serializes with the Orleans System.Text.Json codec.
/// </summary>
internal static class JsonSerializedTypes
{
    /// <summary>
    /// Check whether a type is serialized with the JSON codec.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check.</param>
    /// <returns>True if the JSON codec handles the type; false if another codec must.</returns>
    public static bool Includes(Type type)
    {
        // Exceptions belong to the Orleans exception codec. System.Text.Json cannot write Exception.TargetSite,
        // so a Cratis exception routed here fails to serialize and the grain response is never delivered -
        // the caller hangs until it times out instead of observing the failure.
        if (typeof(Exception).IsAssignableFrom(type))
        {
            return false;
        }

        // OneOfBase derivatives have their own serializer.
        var current = type;
        while (current != typeof(object) && current is not null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition().Name.Contains("OneOfBase"))
            {
                return false;
            }
            current = current.BaseType;
        }

        // OneOf marker types (e.g. OneOf.Types.None, used as job acknowledgements) have no
        // generated Orleans codec. They must be serializable for failed-partition recovery jobs
        // to start across silo boundaries, so route them through the JSON serializer.
        return type == typeof(JsonObject)
            || type == typeof(JsonSchema)
            || type.Namespace == "OneOf.Types"
            || (type.Namespace?.StartsWith("Cratis") ?? false);
    }
}
