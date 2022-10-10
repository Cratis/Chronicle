// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Serialization;

/// <summary>
/// Exception that gets thrown when multiple types have the same derived type identifier.
/// </summary>
public class AmbiguousDerivedTypeIdentifiers : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AmbiguousDerivedTypeIdentifiers"/> class.
    /// </summary>
    /// <param name="types">Types that have the same identifier.</param>
    public AmbiguousDerivedTypeIdentifiers(IEnumerable<Type> types) : base($"The types '{string.Join(", ", types.Select(_ => _.FullName))}' have the same derived type identifier.")
    {
    }
}
