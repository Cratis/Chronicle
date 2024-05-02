// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Types;

/// <summary>
/// Exception that gets thrown when multiple types are found and not allowed.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MultipleTypesFound"/> class.
/// </remarks>
/// <param name="type">Type that multiple of it.</param>
/// <param name="typesFound">The types that was found.</param>
public class MultipleTypesFound(Type type, IEnumerable<Type> typesFound)
    : ArgumentException($"More than one type found for '{type.FullName}' - types found : [{string.Join(',', typesFound.Select(_ => _.FullName))}]");
