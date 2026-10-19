// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    /// <inheritdoc/>
    public async Task<IdempotentAppendManyResult> AppendManyWithIdempotencyKeys(IEnumerable<EventToAppend> events, IReadOnlyList<IdempotencyKey?> idempotencyKeys, CorrelationId correlationId, IEnumerable<Causation> causation, Identity causedBy, ConcurrencyScopes concurrencyScopes)
    {
        var materialized = events.ToList();
        if (materialized.Count != idempotencyKeys.Count)
        {
            return new(new AppendManyResult { CorrelationId = correlationId, Errors = [new("The idempotency key count must match the event count. Nothing was written.")] });
        }
        var repeated = idempotencyKeys.Where(key => key is not null).GroupBy(key => key!.Value, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        if (repeated is not null)
        {
            return new(new AppendManyResult { CorrelationId = correlationId, Errors = [IdempotencyRefusals.Repeated(repeated.First()!)] });
        }
        var requests = materialized.Select((@event, index) => new KeyedAppend(@event, idempotencyKeys[index])).ToList();
        if (requests.Count > 0) await AppendKeyedBatch(requests, correlationId, causation, causedBy, concurrencyScopes);
        return MergeKeyedResults(requests, correlationId);
    }

    static ConcurrencyScopes RemainingScopes(IEnumerable<KeyedAppend> requests, ConcurrencyScopes scopes) => new(
        requests.Where(request => request.Result is null).Select(request => request.Event.EventSourceId).Distinct()
            .ToDictionary(source => source, scopes.GetFor));

    static IdempotentAppendManyResult MergeKeyedResults(List<KeyedAppend> requests, CorrelationId correlationId)
    {
        var results = requests.Select(request => request.Result!).ToArray();
        var succeeded = results.All(result => result.IsSuccess);
        return new(new AppendManyResult
        {
            CorrelationId = correlationId,
            SequenceNumbers = succeeded ? results.Select(result => result.Result.SequenceNumber).ToArray() : [],
            Errors = results.SelectMany(result => result.Result.Errors).Distinct().ToArray(),
            ConstraintViolations = results.SelectMany(result => result.Result.ConstraintViolations).Distinct().ToArray(),
            ConcurrencyViolations = requests.SelectMany(request => request.ConcurrencyViolations).Distinct().ToArray(),
            ConcurrencyCheckPerformed = results.Any(result => result.Result.ConcurrencyCheckPerformed)
        })
        {
            Outcomes = succeeded ? results.Select(result => result.Outcome).ToArray() : [],
            Conflicts = results.SelectMany(result => result.Conflicts).Distinct().ToArray()
        };
    }
}
