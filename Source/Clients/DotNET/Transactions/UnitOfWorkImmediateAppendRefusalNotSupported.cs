// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when a unit of work cannot refuse immediate appends.
/// </summary>
/// <param name="type">The unsupported implementation.</param>
public class UnitOfWorkImmediateAppendRefusalNotSupported(Type type) : Exception($"Unit of work implementation '{type.FullName}' does not support immediate-append refusal.");
