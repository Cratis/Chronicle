// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class a_catching_up_sequence : an_event_sequence_with_repairable_indexes
{
    protected readonly RepairClock _clock = new();
    protected override EventSequenceNumber? Watermark => null;
    protected override EventSequenceNumber InitialNext => 3;

    void Establish() => _eventSequence.ConstraintIndexTimeProvider = _clock;

    protected Task Tick() => ConstraintRepairTimer.Tick(_silo);
    protected Task Hold(string source, string value) => _claims.Save(source, _uniqueDefinition, 0, Hash(value));
    protected async Task<UniqueConstraintValue[]> Values(string source) => (await _claims.GetFor(source, _uniqueDefinition)).ToArray();
    protected static UniqueConstraintValue Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    protected async Task TransferHistory()
    {
        await Seed(0, "old-holder", email: "value");
        await Seed(1, "old-holder", new("Removed", EventTypeGeneration.First));
        await Seed(2, "new-holder", email: "value");
        await Hold("old-holder", "value");
    }
}
