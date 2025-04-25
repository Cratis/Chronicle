// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Serialization;
using Orleans.Serialization.Cloning;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Represents a deep copier for read-only arrays.
/// </summary>
public class ReadOnlyArrayCopier : IGeneralizedCopier, ITypeFilter
{
    /// <inheritdoc/>
    public object DeepCopy(object input, CopyContext context)
    {
        var itemType = input.GetType().GetGenericArguments()[0];
        var listType = typeof(List<>).MakeGenericType(itemType);
        return Activator.CreateInstance(listType, input)!;
    }

    /// <inheritdoc/>
    public bool IsSupportedType(Type type) => IsTypeAllowed(type) ?? false;

    /// <inheritdoc/>
    public bool? IsTypeAllowed(Type type)
    {
        if (type.Name.Contains("ReadOnlyArray")) return true;

        return null;
    }
}
