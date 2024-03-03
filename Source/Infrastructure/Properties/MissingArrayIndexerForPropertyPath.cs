// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Properties;

/// <summary>
/// Exception that gets thrown when a <see cref="ArrayIndexer"/> is missing for a <see cref="PropertyPath"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingArrayIndexerForPropertyPath"/> class.
/// </remarks>
/// <param name="propertyPath"><see cref="PropertyPath"/> it is missing for.</param>
public class MissingArrayIndexerForPropertyPath(PropertyPath propertyPath) : Exception($"Missing array indexer for '{propertyPath}'")
{
}
