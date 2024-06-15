// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Exception that gets thrown when removal has already been defined.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RemovalAlreadyDefined"/> class.
/// </remarks>
/// <param name="type">The type of projection.</param>
public class RemovalAlreadyDefined(Type type)
    : Exception($"Removal already defined for projection '{type.FullName}'. You can only define one event type to be the removal event type.");
