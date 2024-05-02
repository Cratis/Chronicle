// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts;

/// <summary>
/// Exception that gets thrown when a <see cref="Type"/> is not a <see cref="ConceptAs{T}"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TypeIsNotAConcept"/> class.
/// </remarks>
/// <param name="type"><see cref="Type"/> that is not a concept.</param>
public class TypeIsNotAConcept(Type type)
    : Exception($"Type '{type.AssemblyQualifiedName}' is not a concept - implement ConceptAs<> for it to be one.");
