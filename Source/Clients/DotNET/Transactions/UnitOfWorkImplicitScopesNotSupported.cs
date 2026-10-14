// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when a unit cannot enroll implicit scopes.
/// </summary>
/// <param name="implementation">The implementation type.</param>
public class UnitOfWorkImplicitScopesNotSupported(Type implementation)
    : Exception($"Unit of work '{implementation.FullName}' does not support implicit scopes.");
