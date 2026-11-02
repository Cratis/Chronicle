// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents the one <see cref="IJobAdmission"/> Chronicle registers, deciding whether an observer job may run by
/// asking every <see cref="IObserverJobAdmissionRule"/> in order.
/// </summary>
/// <remarks>
/// A job that does not run an <see cref="IObserverJobRequest"/> is not an observer job and is admitted without asking.
/// An observer job is admitted only if every rule admits it, and the first refusal decides. A rule that throws is not
/// a refusal: the exception propagates, and Orleans treats the admission as failed and asks again later.
/// </remarks>
/// <param name="storage">The <see cref="IStorage"/> the rules read from.</param>
/// <param name="rules">The <see cref="IObserverJobAdmissionRule"/> instances to ask.</param>
/// <exception cref="DuplicateObserverJobAdmissionRuleOrder">Thrown when two rules share an order.</exception>
public class ObserverJobAdmission(IStorage storage, IEnumerable<IObserverJobAdmissionRule> rules) : IJobAdmission
{
    readonly IReadOnlyList<IObserverJobAdmissionRule> _rules = Ordered(rules);

    /// <inheritdoc/>
    public async Task<bool> MayRun(JobAdmissionRequest request, CancellationToken cancellationToken)
    {
        if (request.Request is not IObserverJobRequest observerJobRequest)
        {
            return true;
        }

        var context = new ObserverJobAdmissionContext(request, observerJobRequest, storage);
        foreach (var rule in _rules)
        {
            if (!await rule.MayRun(context, cancellationToken))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Orders rules by their <see cref="IObserverJobAdmissionRule.Order"/>.
    /// </summary>
    /// <param name="rules">The rules to order.</param>
    /// <returns>The rules, lowest order first.</returns>
    /// <exception cref="DuplicateObserverJobAdmissionRuleOrder">Thrown when two rules share an order.</exception>
    internal static IReadOnlyList<IObserverJobAdmissionRule> Ordered(IEnumerable<IObserverJobAdmissionRule> rules)
    {
        var ordered = rules.OrderBy(_ => _.Order).ToArray();
        var duplicate = ordered.GroupBy(_ => _.Order).FirstOrDefault(_ => _.Count() > 1);
        if (duplicate is not null)
        {
            throw new DuplicateObserverJobAdmissionRuleOrder(duplicate.Key, duplicate.Select(_ => _.GetType().Name));
        }

        return ordered;
    }
}
