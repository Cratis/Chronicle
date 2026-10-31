// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Chronicle.Transactions;

/// <summary>Lets exactly one caller take over completing the request's unit of work from the request middleware.</summary>
/// <remarks>Retrieve it from the request's feature collection. Once claimed, the middleware never commits, rolls back or reports on the unit.</remarks>
public interface IUnitOfWorkCompletionOwnershipFeature
{
    /// <summary>Gets a value indicating whether the unit of work has been claimed.</summary>
    bool IsClaimed { get; }

    /// <summary>Claims completion of the unit of work.</summary>
    /// <returns>The <see cref="IUnitOfWorkCompletionClaim"/>.</returns>
    /// <exception cref="UnitOfWorkCompletionAlreadyClaimed">The unit has already been claimed.</exception>
    IUnitOfWorkCompletionClaim Claim();

    /// <summary>Tries to claim completion of the unit of work.</summary>
    /// <param name="claim">The claim when successful.</param>
    /// <returns><see langword="true"/> if claimed; <see langword="false"/> if already claimed.</returns>
    bool TryClaim([NotNullWhen(true)] out IUnitOfWorkCompletionClaim? claim);
}
