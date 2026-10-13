// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when a unit-of-work implementation does not support enrollment contributors.
/// </summary>
/// <param name="unitOfWorkType">The implementation that cannot attach a contributor.</param>
public class UnitOfWorkEnrollmentContributorsNotSupported(Type unitOfWorkType)
    : Exception($"The unit of work implementation '{unitOfWorkType.FullName}' does not support enrollment contributors.");
