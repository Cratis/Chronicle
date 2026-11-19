// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_SerializedStateReads;

public class when_a_read_throws : Specification
{
    SerializedStateReads _reads;
    Exception? _error;
    bool _nextReadPerformed;

    void Establish() => _reads = new();

    async Task Because()
    {
        try
        {
            await _reads.Read(() => throw new InvalidOperationException("boom"));
        }
        catch (InvalidOperationException ex)
        {
            _error = ex;
        }

        await _reads.Read(() =>
        {
            _nextReadPerformed = true;
            return Task.CompletedTask;
        });
    }

    [Fact] void should_surface_the_failure_to_its_own_caller() => _error.ShouldNotBeNull();
    [Fact] void should_let_the_next_read_proceed() => _nextReadPerformed.ShouldBeTrue();
}
