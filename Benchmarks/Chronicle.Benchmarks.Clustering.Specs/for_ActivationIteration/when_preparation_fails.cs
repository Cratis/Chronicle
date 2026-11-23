// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_ActivationIteration;

public class when_preparation_fails : Specification
{
    ActivationIteration _iteration = null!;
    Exception? _preparationError;
    Exception? _verificationError;
    bool _verified;
    bool _captured;

    void Establish() => _iteration = new(
        () => Task.CompletedTask,
        () => Task.FromException(new TimeoutException()),
        () => Task.CompletedTask,
        () => _captured = true);

    async Task Because()
    {
        _preparationError = await Catch.Exception(() => _iteration.Prepare(GrainActivationMode.Cold));
        _verificationError = Catch.Exception(() => _iteration.Verify(() => _verified = true));
    }

    [Fact] void should_propagate_the_lifecycle_failure() => _preparationError.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_reject_measurement_validation() => _verificationError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_capture_prepared_evidence() => _captured.ShouldBeFalse();
    [Fact] void should_not_validate_the_operation() => _verified.ShouldBeFalse();
}
