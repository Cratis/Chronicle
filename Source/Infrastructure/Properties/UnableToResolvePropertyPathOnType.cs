// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Properties;

/// <summary>
/// Exception that gets thrown when property path is not possible to be resolved on a type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UnableToResolvePropertyPathOnType"/> class.
/// </remarks>
/// <param name="type">Type that does not hold the property path.</param>
/// <param name="path">The <see cref="PropertyPath"/> that is not possible to resolve.</param>
public class UnableToResolvePropertyPathOnType(Type type, PropertyPath path) : Exception($"Unable to resolve property path '${path}' on type '${type.FullName}'")
{
}
