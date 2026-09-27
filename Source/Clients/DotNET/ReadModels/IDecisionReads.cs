// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>Reads models with guards for a subsequent append.</summary>
public interface IDecisionReads
{
    /// <summary>Reads and enrolls into the ambient unit of work.</summary>
    /// <typeparam name="T">The read model type.</typeparam>
    /// <param name="key">The raw source key.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>The guarded read.</returns>
    Task<DecisionRead<T>> Get<T>(ReadModelKey key, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Reads without enrolling; pass the result to a unit of work or guarded append.</summary>
    /// <typeparam name="T">The read model type.</typeparam>
    /// <param name="key">The raw source key.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>The guarded read.</returns>
    Task<DecisionRead<T>> GetDetached<T>(ReadModelKey key, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Checks the projection shape without I/O.</summary>
    /// <typeparam name="T">The read model type.</typeparam>
    /// <returns>The admission result.</returns>
    DecisionReadAdmission Admit<T>()
        where T : class;
}
