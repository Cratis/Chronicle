// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.MongoDB;

/// <summary>
/// Exception that gets thrown when an unknown type is encountered.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UnknownType"/> class.
/// </remarks>
/// <param name="type">String representation of the unknown type.</param>
public class UnknownType(string type) : Exception($"Unknown type: {type}");
