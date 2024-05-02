// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Types;

/// <summary>
/// Exception that gets thrown when a type is not possible to be resolved by its name.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UnableToResolveTypeByName"/> class.
/// </remarks>
/// <param name="typeName">Name of the type that was not possible to resolve.</param>
public class UnableToResolveTypeByName(string typeName) : ArgumentException($"Unable to resolve '{typeName}'.");
