// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Integration.for_Imports;

public static class Customers
{
    public static readonly DateTimeOffset Epoch = new(2015, 3, 1, 0, 0, 0, TimeSpan.Zero);

    public static async IAsyncEnumerable<EventForEventSourceId> Stream(string prefix, int count, bool keyed = false)
    {
        for (var index = 0; index < count; index++)
        {
            await Task.CompletedTask;
            yield return new EventForEventSourceId($"{prefix}-{index}", new CustomerImported($"{prefix}-{index}"), new Causation(Epoch, "LegacySystem", new Dictionary<string, string> { ["row"] = index.ToString() }))
            {
                Occurred = Epoch.AddMinutes(index),
                SourceSequenceNumber = (ulong)(1000 + index),
                IdempotencyKey = keyed ? new IdempotencyKey($"{prefix}-{index}") : null
            };
        }
    }

    public static IEnumerable<EventForEventSourceId> Emails(string prefix, int count, int duplicateAt) =>
        Enumerable.Range(0, count).Select(index => new EventForEventSourceId(
            $"{prefix}-{index}",
            new CustomerEmailImported(index == duplicateAt ? $"{prefix}-0@example.com" : $"{prefix}-{index}@example.com")));
}
