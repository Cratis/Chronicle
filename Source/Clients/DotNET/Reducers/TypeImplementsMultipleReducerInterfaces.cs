// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// The exception that is thrown when a type implements multiple closed reducer interfaces.
/// </summary>
/// <param name="type">The reducer type.</param>
/// <param name="readModelTypes">The read model types targeted by the reducer.</param>
public class TypeImplementsMultipleReducerInterfaces(Type type, IEnumerable<Type> readModelTypes)
    : Exception($"Type '{type.FullName}' implements multiple IReducerFor<> interfaces for read models: {string.Join(", ", readModelTypes.Select(_ => _.FullName))}. Split it into one reducer per read model.");
