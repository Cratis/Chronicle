// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Json;

/// <summary>
/// Exception that gets thrown when a property path would write through a collection without addressing its elements.
/// </summary>
/// <param name="segment">The path segment that holds the collection.</param>
public class CannotWritePropertyPathThroughCollection(string segment)
    : Exception($"The property '{segment}' holds a collection. Address its elements with '{segment}[]' rather than writing through it, which would replace the collection.");
