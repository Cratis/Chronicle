// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Runs the same digest version dispatch contract against each registry's real backing state: a mutation and a receipt
/// written under the unkeyed version before the switch to the keyed write version stay readable, and retry exactly, after
/// it; receipts written under the keyed version round-trip; and a version that is no longer supported fails closed.
/// </summary>
/// <param name="firstRequest">The mutation written under the unkeyed version, before the switch.</param>
/// <param name="nextRequest">A different mutation on the same sequence, written under the keyed write version.</param>
/// <param name="proposedTarget">The range proposed for both mutations.</param>
public sealed class DigestVersionConformance(
    EventSequenceMutationRequest firstRequest,
    EventSequenceMutationRequest nextRequest,
    EventSequenceMutationTarget proposedTarget)
{
    /// <summary>
    /// The version receipts were written with before the switch.
    /// </summary>
    public static readonly EventSequenceMutationDigestVersion FirstVersion = EventSequenceMutationDigestVersion.Sha256DomainSeparated;

    /// <summary>
    /// The keyed write version.
    /// </summary>
    public static readonly EventSequenceMutationDigestVersion SecondVersion = EventSequenceMutationDigestVersion.HmacSha256PerEventStore;

    EventSequenceMutationDigestVersion _firstVersion;
    EventSequenceMutationDigestVersion _writeVersion;
    EventSequenceMutationBeginResult _activeResumed = null!;
    EventSequenceMutationRegistryArchiveResult _firstArchived = null!;
    EventSequenceMutationBeginResult _nextReserved = null!;
    EventSequenceMutationRegistryArchiveResult _nextArchived = null!;
    EventSequenceMutationRegistryArchiveResult _firstRetry = null!;
    EventSequenceMutationBeginResult _firstBegin = null!;
    EventSequenceMutationRegistryArchiveResult _nextRetryAfterSwitchBack = null!;
    EventSequenceMutationRegistryArchiveResult _nextRetryUnsupported = null!;
    EventSequenceMutationBeginResult _nextBeginUnsupported = null!;
    EventSequenceMutationRegistryArchiveResult _firstRetryWhileSecondUnsupported = null!;

    /// <summary>
    /// Writes the first mutation under the unkeyed version, switches to the keyed write version, and retries every step
    /// exactly.
    /// </summary>
    /// <param name="registry">The registry that writes the state.</param>
    /// <param name="recreated">Creates a new wrapper over the same backing state.</param>
    /// <returns>The asynchronous run.</returns>
    public async Task Run(IEventSequenceMutationRegistry registry, Func<IEventSequenceMutationRegistry> recreated)
    {
        var target = firstRequest.TargetSequence;
        _firstVersion = FirstVersion;
        EventSequenceMutationStateToken firstToken;
        using (EventSequenceMutationDigestSeam.SetCurrentForTest(FirstVersion))
        {
            firstToken = await Commit(registry, firstRequest, _ => { });
        }

        // The production write version is the keyed one. The active head was written under the unkeyed version and is
        // resumed, then archived, under the keyed write version.
        _writeVersion = EventSequenceMutationDigestCalculator.CurrentDigestVersion;
        _activeResumed = await recreated().Begin(firstRequest, proposedTarget);
        _firstArchived = await recreated().Archive(target, firstToken);

        var nextToken = await Commit(registry, nextRequest, begin => _nextReserved = begin);
        _nextArchived = await recreated().Archive(target, nextToken);

        _firstRetry = await recreated().Archive(target, firstToken);
        _firstBegin = await recreated().Begin(firstRequest, proposedTarget);

        using (EventSequenceMutationDigestSeam.SetCurrentForTest(FirstVersion))
        {
            // Back on the unkeyed write version the keyed receipt is still read with the keyed version.
            _nextRetryAfterSwitchBack = await recreated().Archive(target, nextToken);

            // The keyed version is no longer supported: its receipt can neither be retried nor resumed, and the first still can.
            using (EventSequenceMutationDigestSeam.WithoutVersionForTest(SecondVersion))
            {
                _nextRetryUnsupported = await recreated().Archive(target, nextToken);
                _nextBeginUnsupported = await recreated().Begin(nextRequest, proposedTarget);
                _firstRetryWhileSecondUnsupported = await recreated().Archive(target, firstToken);
            }
        }
    }

    /// <summary>Requires the keyed version to be the production write version.</summary>
    public void ShouldWriteWithTheKeyedVersion() => _writeVersion.ShouldEqual(SecondVersion);

    /// <summary>Requires the active head, written under the first version, to resume under the second write version.</summary>
    public void ShouldResumeTheActiveHead() => _activeResumed.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Resumed);

    /// <summary>Requires the resumed head to keep the version it was written with.</summary>
    public void ShouldKeepTheActiveHeadVersion() => _activeResumed.Active!.Definition.DefinitionDigestVersion.ShouldEqual(_firstVersion);

    /// <summary>Requires the first receipt to be written with the version of its definition, not the new write version.</summary>
    public void ShouldArchiveUnderTheDefinitionVersion() => _firstArchived.History!.TerminalWitness.DigestVersion.ShouldEqual(_firstVersion);

    /// <summary>Requires a mutation reserved after the switch to be written with the new write version.</summary>
    public void ShouldReserveUnderTheNewWriteVersion() => _nextReserved.Active!.Definition.DefinitionDigestVersion.ShouldEqual(SecondVersion);

    /// <summary>Requires the receipt of that mutation to carry the new write version.</summary>
    public void ShouldArchiveTheNextUnderTheNewWriteVersion() => _nextArchived.History!.TerminalWitness.DigestVersion.ShouldEqual(SecondVersion);

    /// <summary>Requires the exact retry of the first-version receipt to be already archived.</summary>
    public void ShouldRetryTheFirstReceiptExactly() => _firstRetry.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.AlreadyArchived);

    /// <summary>Requires the exact retry to return the original receipt.</summary>
    public void ShouldReturnTheOriginalFirstReceipt() => _firstRetry.History.ShouldEqual(_firstArchived.History);

    /// <summary>Requires a begin retry of the first-version receipt to replay the archived registration.</summary>
    public void ShouldReplayTheFirstRegistration() => _firstBegin.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Archived);

    /// <summary>Requires the replayed registration to carry the original receipt.</summary>
    public void ShouldReplayTheOriginalFirstReceipt() => _firstBegin.History.ShouldEqual(_firstArchived.History);

    /// <summary>Requires the second-version receipt to retry exactly after the write version switches back.</summary>
    public void ShouldRetryTheSecondReceiptAfterSwitchingBack() => _nextRetryAfterSwitchBack.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.AlreadyArchived);

    /// <summary>Requires a receipt of a no longer supported version to fail closed on retry.</summary>
    public void ShouldFailClosedOnAnUnsupportedReceiptRetry() => _nextRetryUnsupported.Outcome.ShouldNotEqual(EventSequenceMutationRegistryArchiveOutcome.AlreadyArchived);

    /// <summary>Requires a begin retry of a no longer supported version to be corrupt.</summary>
    public void ShouldFailClosedOnAnUnsupportedBegin() => _nextBeginUnsupported.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Corrupt);

    /// <summary>Requires the first-version receipt to remain retryable while the second version is unsupported.</summary>
    public void ShouldKeepTheFirstReceiptReadable() => _firstRetryWhileSecondUnsupported.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.AlreadyArchived);

    async Task<EventSequenceMutationStateToken> Commit(
        IEventSequenceMutationRegistry registry,
        EventSequenceMutationRequest request,
        Action<EventSequenceMutationBeginResult> observeBegin)
    {
        var target = request.TargetSequence;
        var begin = await registry.Begin(request, proposedTarget);
        begin.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Reserved);
        observeBegin(begin);
        var applying = await registry.Transition(target, begin.Token!, EventSequenceMutationTransition.BeginApplying);
        var verifying = await registry.Transition(target, applying.Token!, EventSequenceMutationTransition.BeginVerifying);
        var committed = await registry.Transition(target, verifying.Token!, EventSequenceMutationTransition.CommitSourceWithoutRepair);
        committed.Outcome.ShouldEqual(EventSequenceMutationRegistryTransitionOutcome.Applied);
        return committed.Token!;
    }
}
