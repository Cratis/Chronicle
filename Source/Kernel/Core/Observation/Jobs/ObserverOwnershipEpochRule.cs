// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents the <see cref="IObserverJobAdmissionRule"/> refusing an observer job whose ownership epoch is not the
/// epoch of the observer's persisted claim for the job's scope.
/// </summary>
/// <remarks>
/// <para>
/// The observer grants and persists a claim before it starts a job, and stamps the claim's epoch on the request. A start
/// delayed past a later grant, a start repeated after the job's state was deleted, or a resume of a job whose work the
/// observer has since handed to another, carries an epoch that is no longer the persisted one, and is refused.
/// </para>
/// <para>
/// A request without an epoch is not checked, so jobs persisted before requests carried one keep running, and neither
/// is a job that runs under no claim, such as a retry of a failed partition. The rule reads observer state only.
/// </para>
/// </remarks>
public class ObserverOwnershipEpochRule : IObserverJobAdmissionRule
{
    /// <summary>
    /// The order of the rule: first in the chain.
    /// </summary>
    public const int RuleOrder = 100;

    /// <inheritdoc/>
    public int Order => RuleOrder;

    /// <inheritdoc/>
    public async Task<bool> MayRun(ObserverJobAdmissionContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;
        if (request.OwnershipEpoch is not ulong epoch)
        {
            return true;
        }

        var scope = ObserverJobScope.Of(request);
        if (!scope.IsClaimed)
        {
            return true;
        }

        var key = request.ObserverKey;
        var state = await context.Storage
            .GetEventStore(key.EventStore)
            .GetNamespace(key.Namespace)
            .Observers.Get(key.ObserverId);
        return scope.EpochIn(state)?.Value == epoch;
    }
}
