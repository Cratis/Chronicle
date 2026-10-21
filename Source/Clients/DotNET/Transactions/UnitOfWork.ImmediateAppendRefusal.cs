// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

public partial class UnitOfWork
{
    /// <summary>
    /// The caller owns disposal. The unit retains only a factory for non-owning leases,
    /// so completing or disposing the unit cannot shorten the caller's refusal lifetime.
    /// </summary>
    Func<ImmediateAppendRefusal>? _immediateAppendRefusalLease;
    bool _immediateAppendRefusalOwnsCompletion;

    /// <inheritdoc/>
    public ImmediateAppendRefusal RefuseImmediateAppends()
    {
        ImmediateAppendGuard.RequireUnit(this);
        ReadProtectionGuard.RequireUnit(this, false);
        lock (_decisionLock)
        {
            ThrowIfMutatedDuringEnrollment();
            ThrowIfEnrollmentNotificationFailed();
            ThrowIfUnitOfWorkIsCompleted();
            if (_completing) throw new UnitOfWorkIsCompleting(CorrelationId);
            if (_immediateAppendRefusalLease is not null) return _immediateAppendRefusalLease();
            _immediateAppendRefusalOwnsCompletion = _commitOwner is null && !IsEnforcingReadProtection;
            var frame = new ImmediateAppendRefusal(
                this,
                _immediateAppendRefusalOwnsCompletion,
                owner => CommitCore(true, owner),
                owner => RollbackCore(null, true, owner),
                DisposeImmediateAppendRefusal);
            _immediateAppendRefusalLease = frame.Borrow;
            return frame;
        }
    }

    void RequireImmediateAppendOwner(ImmediateAppendRefusal? owner)
    {
        ImmediateAppendGuard.RequireUnit(this);
        if (_immediateAppendRefusalOwnsCompletion &&
            (owner is null || !ReferenceEquals(owner.UnitOfWork, this) || !ReferenceEquals(owner, ImmediateAppendRefusal.Current)))
        {
            throw new ImmediateAppendRefusalRequiresOwner();
        }
    }

    void DisposeImmediateAppendRefusal(ImmediateAppendRefusal owner)
    {
        lock (_decisionLock)
        {
            ThrowIfMutatedDuringEnrollment();
            if (!_immediateAppendRefusalOwnsCompletion)
            {
                _immediateAppendRefusalLease = null;
                return;
            }
            RequireImmediateAppendOwner(owner);
            if (IsCompleted) return;
            if (_completing) throw new UnitOfWorkIsCompleting(CorrelationId);
            using var span = _activitySource.Rollback(CorrelationId.ToString());
            CompleteRollback();
        }
        _onCompleted(this);
    }
}
