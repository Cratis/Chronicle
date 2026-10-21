// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Contributes a required scope before events are enrolled in a unit of work.
/// </summary>
public interface IUnitOfWorkEnrollmentContributor
{
    /// <summary>
    /// Gets the scope this enrollment must carry.
    /// </summary>
    /// <param name="enrollment">The proposed enrollment.</param>
    /// <returns>The required scope, or <see cref="ConcurrencyScope.NotSet"/> for no requirement.</returns>
    ConcurrencyScope GetRequiredScope(UnitOfWorkEnrollment enrollment);

    /// <summary>
    /// Records a successfully staged enrollment. Implementations must not mutate the unit of work.
    /// </summary>
    /// <param name="enrollment">The staged enrollment.</param>
    /// <param name="effectiveScope">The scope carried by the enrollment.</param>
    void Enrolled(UnitOfWorkEnrollment enrollment, ConcurrencyScope effectiveScope)
    {
    }
}
