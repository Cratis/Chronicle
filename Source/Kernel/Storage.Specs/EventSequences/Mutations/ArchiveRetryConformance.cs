// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Runs the same permanent archive retry contract against each registry's real backing state.
/// Wrapper recreation is not a process-crash test.
/// </summary>
/// <param name="firstRequest">The mutation to archive.</param>
/// <param name="nextRequest">A different mutation targeting the same sequence.</param>
/// <param name="proposedTarget">The range proposed for both mutations.</param>
public sealed class ArchiveRetryConformance(
    EventSequenceMutationRequest firstRequest,
    EventSequenceMutationRequest nextRequest,
    EventSequenceMutationTarget proposedTarget)
{
    EventSequenceMutationStateToken _terminalToken = null!;
    EventSequenceMutationRegistration _registration = null!;
    EventSequenceMutationHistoryEntry _originalReceipt = null!;
    EventSequenceMutationBeginResult _next = null!;
    EventSequenceMutationBeginResult _resumed = null!;
    EventSequenceMutationRegistryArchiveResult _retry = null!;

    /// <summary>
    /// Fully archives the first mutation and reuses the freed head for the next mutation.
    /// Every precondition is checked so a failed setup cannot masquerade as a retry defect.
    /// </summary>
    /// <param name="registry">The original wrapper over the provider's backing state.</param>
    /// <returns>The asynchronous setup.</returns>
    public async Task Establish(IEventSequenceMutationRegistry registry)
    {
        nextRequest.TargetSequence.ShouldEqual(firstRequest.TargetSequence);
        (nextRequest.Id != firstRequest.Id).ShouldBeTrue();
        var target = firstRequest.TargetSequence;
        var begin = await registry.Begin(firstRequest, proposedTarget);
        begin.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Reserved);
        var applying = await registry.Transition(target, begin.Token!, EventSequenceMutationTransition.BeginApplying);
        applying.Outcome.ShouldEqual(EventSequenceMutationRegistryTransitionOutcome.Applied);
        var verifying = await registry.Transition(target, applying.Token!, EventSequenceMutationTransition.BeginVerifying);
        verifying.Outcome.ShouldEqual(EventSequenceMutationRegistryTransitionOutcome.Applied);
        var committed = await registry.Transition(target, verifying.Token!, EventSequenceMutationTransition.CommitSourceWithoutRepair);
        committed.Outcome.ShouldEqual(EventSequenceMutationRegistryTransitionOutcome.Applied);
        _terminalToken = committed.Token!;
        var archived = await registry.Archive(target, _terminalToken);
        archived.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.Archived);
        _originalReceipt = archived.History!;
        _registration = new(committed.Active!.Definition, EventSequenceMutationRegistryLifecycle.Archived, _originalReceipt.Ordinal, _originalReceipt.TerminalWitness);
        EventSequenceMutationValidator.ValidateArchivedRegistration(_terminalToken.Scope, _registration, _originalReceipt).IsValid.ShouldBeTrue();

        // A successful second reservation proves that the first archive actually released the head.
        _next = await registry.Begin(nextRequest, proposedTarget);
        _next.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Reserved);
        _next.Active!.Id.ShouldEqual(nextRequest.Id);
        _next.Active.Ordinal.Value.ShouldEqual(_originalReceipt.Ordinal.Value + 1);
    }

    /// <summary>
    /// Retries the exact original terminal token after the caller recreates the registry wrapper.
    /// Resuming the next request observes whether the retry disturbed its persisted state.
    /// </summary>
    /// <param name="recreatedRegistry">A new wrapper over the same backing state, not a fresh store.</param>
    /// <returns>The asynchronous retry and observation.</returns>
    public async Task RetryArchive(IEventSequenceMutationRegistry recreatedRegistry)
    {
        _retry = await recreatedRegistry.Archive(firstRequest.TargetSequence, _terminalToken);
        _resumed = await recreatedRegistry.Begin(nextRequest, proposedTarget);
    }

    /// <summary>
    /// Requires permanent idempotence after the active head has been reused.
    /// </summary>
    public void ShouldBeAlreadyArchived() => _retry.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.AlreadyArchived);

    /// <summary>
    /// Requires the exact original receipt, including its terminal witness and digest.
    /// </summary>
    public void ShouldReturnOriginalReceipt() => _retry.History.ShouldEqual(_originalReceipt);

    /// <summary>
    /// Validates the retry receipt against the original archived definition and lineage.
    /// </summary>
    public void ShouldReturnVerifiedReceipt() => EventSequenceMutationValidator.ValidateArchivedRegistration(_terminalToken.Scope, _registration, _retry.History).IsValid.ShouldBeTrue();

    /// <summary>
    /// Requires the next operation to remain resumable rather than reserved again or archived.
    /// </summary>
    public void ShouldKeepNextMutationResumable() => _resumed.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Resumed);

    /// <summary>
    /// Requires the next operation's complete compare-and-swap token to remain unchanged.
    /// </summary>
    public void ShouldKeepNextToken() => _resumed.Token.ShouldEqual(_next.Token);

    /// <summary>
    /// Requires the next operation's ordinal to remain unchanged.
    /// </summary>
    public void ShouldKeepNextOrdinal() => _resumed.Active!.Ordinal.ShouldEqual(_next.Active!.Ordinal);

    /// <summary>
    /// Requires the next operation's immutable definition and state to remain unchanged.
    /// </summary>
    public void ShouldKeepNextMutation() => _resumed.Active.ShouldEqual(_next.Active);
}
#endif
