// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Reads the names in use from every <see cref="IResourceInventory"/>, quarantines the owners of overlaps and publishes the outcome.
/// </summary>
/// <param name="inventories">The <see cref="IResourceInventory"/> instances.</param>
/// <param name="snapshot">The <see cref="ResourceNameSnapshot"/> to publish to.</param>
/// <param name="gate">The <see cref="IResourceNameGate"/> told about the existing owners.</param>
/// <param name="exemptions">The <see cref="ResourceNameExemptions"/>.</param>
/// <param name="logger">The <see cref="ILogger{T}"/>.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
public class ResourceNameAudit(
    IEnumerable<IResourceInventory> inventories,
    ResourceNameSnapshot snapshot,
    IResourceNameGate gate,
    ResourceNameExemptions exemptions,
    ILogger<ResourceNameAudit> logger,
    TimeProvider timeProvider)
{
    static readonly TimeSpan _initialDelay = TimeSpan.FromMilliseconds(250);
    static readonly TimeSpan _maximumDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Run the audit. The snapshot stays not ready unless the outcome has been published.
    /// </summary>
    /// <param name="mode">The <see cref="NameCollisionHandling"/>.</param>
    /// <param name="timeout">The time budget for reading every inventory.</param>
    /// <param name="isTransient">Tells whether a failure is worth retrying.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="ResourceNameAuditFailed">The inventories could not be read within the time budget.</exception>
    /// <exception cref="ResourceNameCollisionsFound">Names overlap and <paramref name="mode"/> is <see cref="NameCollisionHandling.Refuse"/>.</exception>
    public async Task RunAsync(NameCollisionHandling mode, TimeSpan timeout, Func<Exception, bool> isTransient, CancellationToken cancellationToken)
    {
        var uses = await Read(timeout, isTransient, cancellationToken);
        var result = ResourceNameClassifier.Classify(uses, exemptions);

        if (mode == NameCollisionHandling.Refuse && result.Overlaps.Count > 0)
        {
            foreach (var overlap in result.Overlaps)
            {
                logger.Overlap(Describe(overlap));
            }

            throw new ResourceNameCollisionsFound(result.Overlaps.Count);
        }

        Report(result);
        Register(uses, result);
        snapshot.Publish(result);
        logger.Completed(result.QuarantinedStores.Count, result.QuarantinedNamespaces.Count, result.Overlaps.Count);
    }

    static string Describe(ResourceOverlap overlap) =>
        string.Join(", ", overlap.Owners.Select(_ => _ is null ? "(kernel)" : ResourceNameText.Escape(_.Namespace is null ? _.EventStore : $"{_.EventStore}#{_.Namespace}")));

    async Task<List<ResourceUse>> Read(TimeSpan timeout, Func<Exception, bool> isTransient, CancellationToken cancellationToken)
    {
        using var budget = new CancellationTokenSource(timeout, timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, budget.Token);
        Exception? lastTransient = null;
        var uses = new List<ResourceUse>();

        try
        {
            foreach (var inventory in inventories)
            {
                uses.AddRange(await Read(inventory, isTransient, exception => lastTransient = exception, linked.Token));
            }

            return uses;
        }
        catch (OperationCanceledException) when (budget.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            var failure = new ResourceNameAuditFailed(timeout, lastTransient);
            logger.ReadingFailed(failure);
            throw failure;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.ReadingFailed(exception);
            throw;
        }
    }

    async Task<IEnumerable<ResourceUse>> Read(IResourceInventory inventory, Func<Exception, bool> isTransient, Action<Exception> transientFailed, CancellationToken cancellationToken)
    {
        var delay = _initialDelay;
        while (true)
        {
            try
            {
                return await inventory.Read(cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException && isTransient(exception))
            {
                transientFailed(exception);
                logger.ReadingRetried(exception);
                await Task.Delay(delay, timeProvider, cancellationToken);
                delay = delay * 2 > _maximumDelay ? _maximumDelay : delay * 2;
            }
        }
    }

    void Report(ResourceNameAuditResult result)
    {
        foreach (var eventStore in result.QuarantinedStores)
        {
            logger.EventStoreQuarantined(ResourceNameText.Escape(eventStore));
        }

        foreach (var owner in result.QuarantinedNamespaces)
        {
            logger.NamespaceQuarantined(ResourceNameText.Escape(owner.EventStore), ResourceNameText.Escape(owner.Namespace!));
        }

        foreach (var overlap in result.Overlaps.Where(_ => _.Owners.Any(owner => owner is null || ResourceNameClassifier.IsSystem(owner))))
        {
            logger.KernelOverlap(Describe(overlap));
        }
    }

    void Register(IEnumerable<ResourceUse> uses, ResourceNameAuditResult result)
    {
        var stores = result.QuarantinedStores.ToHashSet(StringComparer.Ordinal);
        var namespaces = result.QuarantinedNamespaces.ToHashSet();

        foreach (var owner in uses.Select(_ => _.Owner).OfType<ResourceOwner>().Distinct())
        {
            if (stores.Contains(owner.EventStore) || namespaces.Contains(owner))
            {
                continue;
            }

            if (owner.Namespace is null)
            {
                gate.MarkRegistered(new EventStoreName(owner.EventStore));
            }
            else
            {
                gate.MarkRegistered(new EventStoreName(owner.EventStore), new EventStoreNamespaceName(owner.Namespace));
            }
        }
    }
}
