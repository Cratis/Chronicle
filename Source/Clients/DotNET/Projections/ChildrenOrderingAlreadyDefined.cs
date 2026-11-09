// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// The exception that is thrown when more than one ordering is defined for the same children collection.
/// </summary>
/// <param name="childType">The type of child the ordering was defined for.</param>
public class ChildrenOrderingAlreadyDefined(Type childType)
    : Exception($"An ordering is already defined for the children of type '{childType.FullName}'. Use one of Prepend, OrderBy or OrderByDescending.");
