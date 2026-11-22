// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;

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

        // Kernel payloads are Orleans types with generated serializers; JsonObject, JsonSchema and the shared property
        // types travel as surrogates. What is left on the JSON codec is what Orleans cannot carry without help from a type
        // this repository does not own:
        //  - OneOf marker types (e.g. OneOf.Types.None, used as job acknowledgements) have no generated Orleans codec. They
        //    must be serializable for failed-partition recovery jobs to start across silo boundaries.
        //  - Types owned by Cratis.Choreography (jobs, leases, work results) are not annotated for Orleans.
        //  - Event objects, the types marked with EventTypeAttribute (the kernel's own system events, appended through the event
        //    sequence as an object). An event is open-ended content that the client defines, so there is no member list to
        //    annotate.
        //  - Job step states derive from the Choreography JobStepState, whose members (id, status, status changes) Orleans
        //    can only carry if Choreography annotates it. A serializer generated for the derived type alone would silently
        //    leave them out, so these stay on the JSON codec until Choreography does.
        return type.Namespace == "OneOf.Types"
            || (type.Namespace?.StartsWith("Cratis.Choreography", StringComparison.Ordinal) ?? false)
            || Attribute.IsDefined(type, typeof(EventTypeAttribute), inherit: false)
            || typeof(JobStepState).IsAssignableFrom(type);
    }
}
