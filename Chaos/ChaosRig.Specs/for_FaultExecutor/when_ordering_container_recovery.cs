// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_FaultExecutor;

/// <summary>
/// Specifies that Mongo resumes restore a potentially self-fenced kernel before dependent workers.
/// </summary>
public class when_ordering_container_recovery : Specification
{
    IReadOnlyList<string> _mongoRecovery;
    IReadOnlyList<string> _kernelRecovery;

    void Because()
    {
        _mongoRecovery = FaultExecutor.RecoveryOrder("mongo", "mongo", "kernel");
        _kernelRecovery = FaultExecutor.RecoveryOrder("kernel", "mongo", "kernel");
    }

    [Fact] void should_restore_mongo_before_its_dependent_kernel() => string.Join(' ', _mongoRecovery).ShouldEqual("mongo kernel");
    [Fact] void should_not_skip_the_kernel_after_a_mongo_resume() => _mongoRecovery.Count.ShouldEqual(2);
    [Fact] void should_not_restart_storage_for_a_kernel_only_fault() => string.Join(' ', _kernelRecovery).ShouldEqual("kernel");
}
