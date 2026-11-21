// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_RunVerdict;

public class when_measured_operations_failed : Specification
{
    RunVerdict _verdict = default!;

    void Because() => _verdict = RunVerdict.Of([], 2);

    [Fact] void should_fail() => _verdict.Succeeded.ShouldBeFalse();
}
