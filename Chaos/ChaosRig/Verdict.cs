// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

internal static class Verdict
{
    public static VerdictResult Compare(VerdictInput input)
    {
        var byId = input.Events.GroupBy(item => item.Id).ToDictionary(group => group.Key, group => group.Count());
        var lost = input.Ledger.Count(item => item.Status == "acked" && !byId.ContainsKey(item.Id));
        var duplicates = byId.Values.Sum(count => count - 1);
        var tail = input.Events.Count == 0 ? 0 : input.Events.Max(item => item.Sequence);
        var behind = input.ExpectedObservers.Count(id => !input.Observers.Any(observer => observer.Id == id && observer.Active && observer.LastHandled >= tail));
        var violations = HealthViolations(input, byId);
        var reactorDuplicates = input.SideLedger.GroupBy(id => id).Sum(group => Math.Max(0, group.Count() - 1));
        return new(lost, duplicates, Gaps(input.Events), behind, ModelMismatches(input.Events, input.Projections), ModelMismatches(input.Events, input.Reductions), input.FailedPartitions, reactorDuplicates, violations);
    }

    static IReadOnlyList<string> HealthViolations(VerdictInput input, Dictionary<string, int> byId)
    {
        var result = new List<string>();
        DrainViolations(input, result);
        LedgerViolations(input, byId, result);
        if (byId.Keys.Except(input.SideLedger).Any()) result.Add("Reactor side-ledger missing events.");
        if (input.SideLedger.Except(byId.Keys).Any()) result.Add("Reactor side-ledger has unknown events.");
        if (input.Events.Select(item => item.Sequence).Distinct().Count() != input.Events.Count) result.Add("Repeated sequence numbers.");
        return result;
    }

    static void DrainViolations(VerdictInput input, List<string> result)
    {
        if (!input.Settled) result.Add("Drain did not stabilize before the settle deadline.");
        if (input.Events.Count == 0) result.Add("No events observed; refusing a vacuous clean verdict.");
        if (!input.Ledger.Any(item => item.Status == "acked")) result.Add("No acknowledged appends.");
        if (input.InFlightPartitions != 0) result.Add($"In-flight partitions: {input.InFlightPartitions}");
        if (input.PendingJobs != 0) result.Add($"Pending jobs: {input.PendingJobs}");
        if (input.ExpectedStateDocuments != input.ExpectedObservers.Count) result.Add("Missing or repeated kernel observer state documents.");
    }

    static void LedgerViolations(VerdictInput input, Dictionary<string, int> byId, List<string> result)
    {
        var intentions = input.Ledger.ToDictionary(item => item.Id);
        if (byId.Keys.Except(intentions.Keys).Any()) result.Add("Events without ledger intentions.");
        var known = input.Events.Where(item => intentions.ContainsKey(item.Id));
        if (known.Any(item => item.Source != intentions[item.Id].Source || item.Payload != intentions[item.Id].Payload)) result.Add("Event content differs from its durable intention.");
    }

    static int Gaps(IReadOnlyList<ObservedEvent> events)
    {
        var sequences = events.Select(item => item.Sequence).Distinct().Order().ToArray();
        var gaps = 0;
        ulong next = 0;
        foreach (var sequence in sequences)
        {
            if (sequence != next) gaps++;
            next = sequence + 1;
        }
        return gaps;
    }

    static int ModelMismatches(IReadOnlyList<ObservedEvent> events, IReadOnlyList<ModelValue> models) => events
        .GroupBy(item => item.Source)
        .Count(group => !models.Any(model => model.Source == group.Key && model.Count == group.Count() && model.Sum == group.Sum(item => item.Payload)));
}
