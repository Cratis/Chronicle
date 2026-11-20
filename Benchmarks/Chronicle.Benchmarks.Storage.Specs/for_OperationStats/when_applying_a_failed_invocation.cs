// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_OperationStats;

public class when_applying_a_failed_invocation : Specification
{
    OperationStats _stats = default!;

    void Because()
    {
        _stats = new();
        _stats.Apply(InvocationRecord.Failure);
    }

    [Fact] void should_count_no_operation() => _stats.Operations.ShouldEqual(0);
    [Fact] void should_count_the_error() => _stats.Errors.ShouldEqual(1);
}
