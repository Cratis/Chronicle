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
    readonly object _claimLock = new();
    Task _completionTail = Task.CompletedTask;
    UnitOfWorkCompletionClaim? _claim;
    UnitOfWorkCompletionResult? _result;

    /// <inheritdoc/>
    public bool IsClaimed => _claim is not null;

    /// <summary>Gets the claim, if the unit has been claimed.</summary>
    internal IUnitOfWorkCompletionClaim? CurrentClaim => _claim;

    /// <summary>Gets a value indicating whether the feature itself completed the unit.</summary>
    internal bool HasCompleted => _result is not null;

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

    static async Task<T> RunAfter<T>(Task previous, Func<Task<T>> work)
    {
        try
        {
            await previous;
        }
        catch
        {
            // The previous completion step reports its own failure to its caller.
        }
        return await work();
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

    async Task<UnitOfWorkCompletionResult> CommitResult() => (await CommitCore()).Result;

    Task<T> Serialize<T>(Func<Task<T>> work)
    {
        lock (_claimLock)
        {
            var task = RunAfter(_completionTail, work);
            _completionTail = task;
            return task;
        }
    }

    Task<(UnitOfWorkCompletionResult Result, bool Created)> CommitCore() => Serialize(async () =>
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
    });

    Task<UnitOfWorkCompletionResult> RollbackResult() => Serialize(async () =>
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
    });
}
