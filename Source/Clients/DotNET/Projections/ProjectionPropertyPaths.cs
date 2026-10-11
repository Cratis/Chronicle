// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Resolves property names within a projection path using the serialization naming policy.
/// </summary>
internal static class ProjectionPropertyPaths
{
    /// <summary>
    /// Applies the naming policy to each property segment without changing path operators.
    /// </summary>
    /// <param name="path">The property path to resolve.</param>
    /// <param name="namingPolicy">The serialization naming policy.</param>
    /// <returns>The serialized property path.</returns>
    public static PropertyPath Resolve(PropertyPath path, INamingPolicy namingPolicy) =>
        new(string.Join('.', path.Segments.Select(segment => ResolveSegment(segment, namingPolicy))));

    static string ResolveSegment(IPropertyPathSegment segment, INamingPolicy namingPolicy) => segment switch
    {
        PropertyName => namingPolicy.GetPropertyName(segment.Value),
        ArrayProperty => $"[{namingPolicy.GetPropertyName(segment.Value)}]",
        _ => segment.ToString()!
    };
}
