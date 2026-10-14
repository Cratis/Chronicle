// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when an implicit guard has no concrete boundary.
/// </summary>
/// <param name="boundary">The invalid boundary.</param>
public class ImplicitScopeBoundaryMustBeConcrete(EventSequenceNumber boundary)
    : Exception($"Implicit scope boundary '{boundary}' must be an actual sequence number or BeforeFirst.");
