// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.AspNetCore.Transactions;

/// <summary>Request-scoped completion handle owned by the middleware.</summary>
/// <param name="unitOfWork">The request unit of work.</param>
/// <param name="owner">The middleware's owner capability, if supported.</param>
internal sealed class UnitOfWorkCompletionFeature(IUnitOfWork unitOfWork, DecisionReadCommitOwner? owner) : IUnitOfWorkCompletionFeature, IUnitOfWorkCompletionOwnershipFeature
{
    readonly SemaphoreSlim _completion = new(1, 1);
    readonly object _claimLock = new();
    UnitOfWorkCompletionClaim? _claim;
    UnitOfWorkCompletionResult? _result;

    /// <summary>Gets the claim, if the unit has been claimed.</summary>
    internal IUnitOfWorkCompletionClaim? CurrentClaim => _claim;

    /// <summary>Gets a value indicating whether the feature itself completed the unit.</summary>
    internal bool HasCompleted => _result is not null;

    /// <inheritdoc/>
    public bool IsClaimed => _claim is not null;

    /// <inheritdoc/>
    public IUnitOfWorkCompletionClaim Claim() => TryClaim(out var claim) ? claim : throw new UnitOfWorkCompletionAlreadyClaimed(unitOfWork.CorrelationId);

    /// <inheritdoc/>
    public bool TryClaim([NotNullWhen(true)] out IUnitOfWorkCompletionClaim? claim)
    {
        lock (_claimLock)
        {
            if (_claim is not null)
            {
                claim = null;
                return false;
            }
            claim = _claim = new UnitOfWorkCompletionClaim(unitOfWork, CommitResult, RollbackResult, () => _result);
            return true;
        }
    }

    /// <inheritdoc/>
    public async Task<IUnitOfWork> CommitAsync()
    {
        if (IsClaimed) throw new UnitOfWorkCompletionAlreadyClaimed(unitOfWork.CorrelationId);
        var (result, created) = await CommitCore();

        // The early-commit contract is to throw a technical failure to the caller that triggered it; a repeat call does not rethrow.
        if (created && result.Outcome == UnitOfWorkCompletionOutcome.Unknown && result.Exception is not null)
        {
            ExceptionDispatchInfo.Capture(result.Exception).Throw();
        }
        return unitOfWork;
    }

    async Task<UnitOfWorkCompletionResult> CommitResult() => (await CommitCore()).Result;

    async Task<(UnitOfWorkCompletionResult Result, bool Created)> CommitCore()
    {
        await _completion.WaitAsync();
        try
        {
            if (_result is not null) return (_result, false);
            try
            {
                if (owner is not null)
                {
                    await ((UnitOfWork)unitOfWork).CommitAsOwner(owner);
                }
                else
                {
                    await unitOfWork.Commit();
                }
                return (_result = UnitOfWorkCompletionResults.FromCommit(unitOfWork), true);
            }
            catch (Exception ex) when (!IsLifecycleMisuse(ex))
            {
                return (_result = UnitOfWorkCompletionResults.FromUnknown(unitOfWork, ex), true);
            }
        }
        finally
        {
            _completion.Release();
        }
    }

    async Task<UnitOfWorkCompletionResult> RollbackResult()
    {
        await _completion.WaitAsync();
        try
        {
            if (_result is not null) return _result;
            if (!unitOfWork.IsCompleted)
            {
                if (owner is not null)
                {
                    await ((UnitOfWork)unitOfWork).RollbackAsOwner(owner);
                }
                else
                {
                    await unitOfWork.Rollback();
                }
            }
            return _result = UnitOfWorkCompletionResults.FromRollback(unitOfWork);
        }
        finally
        {
            _completion.Release();
        }
    }

    static bool IsLifecycleMisuse(Exception exception) => exception is
        ProtectedUnitOfWorkRequiresOwner or
        UnitOfWorkIsCompleted or
        UnitOfWorkIsCompleting or
        UnitOfWorkIsAlreadyCommitted or
        UnitOfWorkIsAlreadyRolledBack or
        UnitOfWorkMutatedDuringEnrollment or
        UnitOfWorkEnrollmentNotificationFailed or
        ProtectedUnitOfWorkEventsAfterCompletion;
}
