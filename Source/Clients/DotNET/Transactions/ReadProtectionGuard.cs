// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Public-entry admission checks consult the invocation, not the ambient unit.
/// </summary>
internal static class ReadProtectionGuard
{
    /// <summary>
    /// Refuses raw reads without an explicit read allowance.
    /// </summary>
    /// <param name="operation">The public read operation.</param>
    /// <exception cref="ReadRefusedWhileReadsAreProtected">No allowance is open.</exception>
    internal static void Read(string operation)
    {
        if (ReadProtectionFrame.Current is not null && !ReadProtectionReadRegion.IsAllowed) throw new ReadRefusedWhileReadsAreProtected(operation);
    }

    /// <summary>
    /// Refuses detached acquisition unless explicitly advisory.
    /// </summary>
    /// <param name="operation">The public detached operation.</param>
    /// <exception cref="DetachedReadWhileReadsAreProtected">The read is not advisory.</exception>
    internal static void Detached(string operation)
    {
        if (ReadProtectionFrame.Current is not null && !ReadProtectionReadRegion.IsAdvisory) throw new DetachedReadWhileReadsAreProtected(operation);
    }

    /// <summary>
    /// Refuses every public immediate append, regardless of read allowances.
    /// </summary>
    /// <param name="operation">The append operation.</param>
    /// <param name="description">The event description, without evaluating event providers.</param>
    /// <exception cref="ImmediateAppendWhileReadsAreProtected">The invocation is protected.</exception>
    internal static void Append(string operation, string description = "events")
    {
        if (ReadProtectionFrame.Current is not null) throw new ImmediateAppendWhileReadsAreProtected(operation, description);
    }

    /// <summary>
    /// Checks the narrow framework permission to complete a stream after commit.
    /// </summary>
    /// <param name="target">The complete target receiving the operation.</param>
    /// <exception cref="ImmediateAppendWhileReadsAreProtected">The frame did not authorize completion of this target.</exception>
    internal static void CompleteStream(ReadProtectionTarget target)
    {
        if (ReadProtectionFrame.Current is not null && !ReadProtectionFrame.CanComplete(target)) throw new ImmediateAppendWhileReadsAreProtected(nameof(CompleteStream), "stream completion");
    }

    /// <summary>
    /// Binds staging, enrollment and completion to the frame's unit.
    /// </summary>
    /// <param name="unit">The receiving unit.</param>
    /// <param name="allowAdvisory">Whether an explicit advisory region permits another unit.</param>
    /// <exception cref="ReadProtectionUnitMismatch">The unit does not own this invocation.</exception>
    internal static void RequireUnit(IUnitOfWork unit, bool allowAdvisory = true)
    {
        var frame = ReadProtectionFrame.Current;
        if (frame is not null && !ReferenceEquals(unit, frame.UnitOfWork) && !(allowAdvisory && ReadProtectionReadRegion.IsAdvisory)) throw new ReadProtectionUnitMismatch();
    }

    /// <summary>
    /// Resolves ambient acquisition without losing the invocation after early completion.
    /// </summary>
    /// <param name="manager">The ambient unit manager.</param>
    /// <param name="description">The protected model.</param>
    /// <returns>The ambient receiving unit.</returns>
    /// <exception cref="DecisionReadRequiresUnitOfWork">No ambient unit exists.</exception>
    internal static IUnitOfWork AmbientUnit(IUnitOfWorkManager manager, string description)
    {
        if (ReadProtectionFrame.Current is { UnitCompleted: true } frame) ProtectedRead(frame.UnitOfWork, description);
        if (!manager.HasCurrent) throw new DecisionReadRequiresUnitOfWork();
        return manager.Current;
    }

    /// <summary>
    /// Checks a protected acquisition before any transport or callback occurs.
    /// </summary>
    /// <param name="unit">The receiving unit.</param>
    /// <param name="description">The protected model or read.</param>
    /// <exception cref="ReadAfterProtectedCommit">The active invocation completed its unit.</exception>
    internal static void ProtectedRead(IUnitOfWork unit, string description)
    {
        RequireUnit(unit);
        if (ReadProtectionFrame.Current is { UnitCompleted: true }) throw new ReadAfterProtectedCommit(description);
    }
}
