// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration;

/// <summary>
/// Names the types a generated projection declares.
/// </summary>
public static class ProjectionNaming
{
    /// <summary>
    /// Resolves the type name to declare a projection as.
    /// </summary>
    /// <param name="identifier">The projection's identifier, which is usually fully qualified.</param>
    /// <param name="readModelName">The name of the read model the projection targets.</param>
    /// <returns>A name that is legal as a type name and distinct from the read model's.</returns>
    /// <remarks>
    /// A projection identifier is a path - <c>Samples.Backoffice.Invoice</c> - and no target language
    /// accepts that as a type name, so only its last segment is used. That segment is frequently the
    /// read model's own name, which would declare two types with one name in the same file, so a
    /// projection that would collide is suffixed instead.
    /// </remarks>
    public static string TypeNameFor(string identifier, string readModelName)
    {
        var index = identifier.LastIndexOf('.');
        var name = index < 0 ? identifier : identifier[(index + 1)..];

        return string.Equals(name, readModelName, StringComparison.Ordinal)
            ? $"{name}Projection"
            : name;
    }
}
