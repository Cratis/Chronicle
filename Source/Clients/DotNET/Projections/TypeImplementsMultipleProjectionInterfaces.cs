// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// The exception that is thrown when a type implements multiple closed projection interfaces.
/// </summary>
/// <param name="type">The projection type.</param>
/// <param name="readModelTypes">The read model types targeted by the projection.</param>
public class TypeImplementsMultipleProjectionInterfaces(Type type, IEnumerable<Type> readModelTypes)
    : Exception($"Type '{type.FullName}' implements multiple IProjectionFor<> interfaces for read models: {string.Join(", ", readModelTypes.Select(_ => _.FullName))}. Split it into one projection per read model.");
