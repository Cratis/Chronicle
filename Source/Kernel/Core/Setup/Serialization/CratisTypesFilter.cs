// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Serialization;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Represents an <see cref="ITypeFilter"/> that allows all Cratis types - the same set that is
/// routed through the JSON serializer. Without it, polymorphic Cratis payloads (interface-typed
/// parameters and arrays of them, such as constraint definitions) are rejected by the Orleans
/// type manifest when a grain call crosses a silo boundary; same-silo calls only deep-copy and
/// never hit the type-name check, which is why this only surfaces in a cluster.
/// </summary>
public class CratisTypesFilter : ITypeFilter
{
    /// <inheritdoc/>
    public bool? IsTypeAllowed(Type type)
    {
        var elementType = type.IsArray ? type.GetElementType()! : type;
        if (elementType.Namespace?.StartsWith("Cratis", StringComparison.Ordinal) ?? false)
        {
            return true;
        }

        return null;
    }
}
