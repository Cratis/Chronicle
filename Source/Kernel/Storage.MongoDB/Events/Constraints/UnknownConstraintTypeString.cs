// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Exception that gets thrown when an unknown constraint type is encountered.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="UnknownConstraintType"/>.
/// </remarks>
/// <param name="type">String representation that is unknown.</param>
public class UnknownConstraintTypeString(string type) : Exception($"Unknown constraint type '{type}'");
