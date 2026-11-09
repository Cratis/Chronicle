// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_MongoDBReservedEventSequenceIds;

public class when_checking : Specification
{
    readonly MongoDBReservedEventSequenceIds _reserved = new();

    bool IsReserved(string id) => _reserved.IsReserved(new Concepts.EventSequences.EventSequenceId(id));

    [Theory]
    [InlineData("observers")]
    [InlineData("event-sequences")]
    [InlineData("jobs")]
    [InlineData("job-steps")]
    [InlineData("occurrences")]
    [InlineData("effect-receipts")]
    [InlineData("observer-definition-change-progress")]
    [InlineData("chronicle_capability_probe")]
    [InlineData("CHRONICLE_CAPABILITY_PROBE")]
    [InlineData("chronicle_capability_probe_0123456789abcdef01234567")]
    [InlineData("CHRONICLE_CAPABILITY_PROBE_0123456789ABCDEF01234567")]
    [InlineData("Observers")]
    [InlineData("orders+closed_streams")]
    [InlineData("event-log+constraint")]
    [InlineData("orders+email+values+constraint")]
    void should_reserve(string id) => IsReserved(id).ShouldBeTrue();

    [Theory]
    [InlineData("orders")]
    [InlineData("my-sequence")]
    [InlineData("orders+archive")]
    [InlineData("orders-changes")]
    [InlineData("price-changes")]
    [InlineData("chronicle_capability_probes")]
    void should_not_reserve(string id) => IsReserved(id).ShouldBeFalse();
}
