// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when a unit of work implementation cannot carry structured named tags.
/// </summary>
/// <param name="implementation">The implementation that cannot carry named tags.</param>
public class UnitOfWorkNamedTagsNotSupported(Type implementation)
    : Exception($"Unit of work implementation '{implementation.FullName}' does not support named tags.");
