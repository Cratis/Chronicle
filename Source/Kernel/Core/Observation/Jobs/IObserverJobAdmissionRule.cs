// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Defines one rule <see cref="ObserverJobAdmission"/> asks before an observer job's execution begins or begins again.
/// </summary>
/// <remarks>
/// <para>
/// Rules are asked in ascending <see cref="Order"/> and an observer job runs only if every rule admits it. The first
/// refusal stops the chain, so a rule that has no opinion about a job admits it rather than deciding for the others.
/// Two rules may not share an order; that is an error at silo startup.
/// </para>
/// <para>
/// A rule reads storage only. It never calls a grain - the job asking holds its own turn while it waits - and it never
/// turns a failure into a refusal: a rule that cannot answer throws, and Orleans treats that as no answer, asked again
/// later, never as a refusal. Orleans asks at a job's start, resume and stage advance, never while it prepares steps.
/// </para>
/// <para>
/// The slots are:
/// <list type="bullet">
/// <item><description>100 - <see cref="ObserverOwnershipEpochRule"/>: refuses a job whose ownership epoch is not the observer's persisted claim (#5382).</description></item>
/// <item><description>200 - reserved for the membership scope rule of decision 0011 (rule 1): a store or observer out of scope, not initialized or awaiting registration has no opinion.</description></item>
/// <item><description>300 - reserved for the initializing rule of decision 0011 (rule 2): refuses every job of an observer key that is initializing.</description></item>
/// <item><description>400 - reserved for the terminal job row rule of decision 0011 (rule 3).</description></item>
/// <item><description>500 - reserved for the membership revision rule of decision 0011 (rules 4 and 5), which owns <see cref="IObserverJobRequest.Revision"/>.</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IObserverJobAdmissionRule
{
    /// <summary>
    /// Gets the position of the rule in the chain. Lower runs first; no two rules may share one.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Decides whether an observer job may run.
    /// </summary>
    /// <param name="context">The <see cref="ObserverJobAdmissionContext"/> describing the job.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> cancelled when the bound for the answer has passed.</param>
    /// <returns>True to admit the job or to have no opinion about it, false to refuse it.</returns>
    Task<bool> MayRun(ObserverJobAdmissionContext context, CancellationToken cancellationToken);
}
