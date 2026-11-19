// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_SerializedStateReads;

public class when_reading_one_after_another : Specification
{
    SerializedStateReads _reads;
    int _performed;

    void Establish() => _reads = new();

    async Task Because()
    {
        await _reads.Read(Count);
        await _reads.Read(Count);
        await _reads.Read(Count);
    }

    Task Count()
    {
        _performed++;
        return Task.CompletedTask;
    }

    [Fact] void should_perform_every_read() => _performed.ShouldEqual(3);
}
