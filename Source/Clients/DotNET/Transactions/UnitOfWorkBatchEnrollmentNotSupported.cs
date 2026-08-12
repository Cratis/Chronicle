// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when a unit-of-work implementation does not support ordered batch enrollment.
/// </summary>
/// <param name="unitOfWorkType">The type of unit of work that does not support ordered batch enrollment.</param>
public class UnitOfWorkBatchEnrollmentNotSupported(Type unitOfWorkType)
    : Exception($"The unit of work implementation '{unitOfWorkType.FullName}' does not support ordered batch enrollment.");
