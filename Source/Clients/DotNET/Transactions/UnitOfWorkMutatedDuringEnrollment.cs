// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when a contributor mutates its unit of work during consultation or notification.
/// </summary>
public class UnitOfWorkMutatedDuringEnrollment()
    : Exception("A unit of work cannot be mutated while enrollment contributors are running.");
