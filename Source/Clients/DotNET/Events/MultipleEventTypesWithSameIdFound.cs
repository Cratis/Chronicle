// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

/// <summary>
/// Exception that gets thrown when multiple event types with the same id is found.
/// </summary>
public class MultipleEventTypesWithSameIdFound : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="MultipleEventTypesWithSameIdFound"/>.
    /// </summary>
    /// <param name="types">The CLR types.</param>
    public MultipleEventTypesWithSameIdFound(IEnumerable<Type> types)
        : base($"Multiple event types with the same id found: {string.Join(", ", types.Select(_ => _.FullName))}")
    {
    }
}
