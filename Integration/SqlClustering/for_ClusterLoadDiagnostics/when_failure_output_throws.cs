// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using NSubstitute;

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoadDiagnostics;

public class when_failure_output_throws : Specification
{
    Exception _original = null!;
    Exception _caught = null!;

    async Task Because()
    {
        try
        {
            try
            {
                ClusterLoad.VerifyObservations([(Guid.NewGuid(), (EventSequenceNumber)7)], []);
            }
            catch (Exception ex)
            {
                _original = ex;
                await using var output = new UnavailableWriter();
                await ClusterLoadDiagnostics.DumpSafely(Substitute.For<IReactorHandler>(), [], [], output);
                throw;
            }
        }
        catch (Exception ex)
        {
            _caught = ex;
        }
    }

    [Fact] public void should_preserve_the_original_preservation_assertion() => ReferenceEquals(_caught, _original).ShouldBeTrue();
    [Fact] public void should_not_turn_the_failed_assertion_into_success() => (_caught is not null).ShouldBeTrue();

    sealed class UnavailableWriter : StringWriter
    {
        public override Task WriteLineAsync(string? value) => Task.FromException(new InvalidOperationException("unavailable diagnostic output"));
    }
}
