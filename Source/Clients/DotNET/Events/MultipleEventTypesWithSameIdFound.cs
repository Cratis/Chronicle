// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Exception that gets thrown when multiple event types with the same id is found.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="MultipleEventTypesWithSameIdFound"/>.
/// </remarks>
/// <param name="types">The CLR types.</param>
public class MultipleEventTypesWithSameIdFound(IEnumerable<Type> types) : Exception($"Multiple event types with the same id found: {string.Join(", ", types.Select(_ => _.FullName))}")
{
}
