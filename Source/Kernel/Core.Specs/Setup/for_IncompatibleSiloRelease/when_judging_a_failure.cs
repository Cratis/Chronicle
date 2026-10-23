// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Serialization;

namespace Cratis.Chronicle.Setup.for_IncompatibleSiloRelease;

public class when_judging_a_failure : Specification
{
    const string UnresolvedInvokable = "Unable to resolve type alias \"(\"inv\",[Orleans.Runtime.GrainReference],[Cratis.Chronicle.Projections.IProjectionsManager,Cratis.Chronicle.Core],\"F436C56F\")\".";

    [Fact] void should_recognize_a_grain_method_the_other_silo_does_not_have() => IncompatibleSiloRelease.IsLikely(new TypeLoadException(UnresolvedInvokable)).ShouldBeTrue();
    [Fact] void should_recognize_it_when_wrapped() => IncompatibleSiloRelease.IsLikely(new OrleansException("call failed", new TypeLoadException(UnresolvedInvokable))).ShouldBeTrue();
    [Fact] void should_recognize_it_inside_an_aggregate() => IncompatibleSiloRelease.IsLikely(new AggregateException(new InvalidOperationException(), new TypeLoadException(UnresolvedInvokable))).ShouldBeTrue();
    [Fact] void should_recognize_a_missing_codec() => IncompatibleSiloRelease.IsLikely(new CodecNotFoundException("Could not find a codec")).ShouldBeTrue();
    [Fact] void should_not_blame_the_cluster_for_a_failure_of_the_work() => IncompatibleSiloRelease.IsLikely(new InvalidOperationException("something is genuinely wrong")).ShouldBeFalse();
    [Fact] void should_not_blame_releases_for_an_unreachable_silo() => IncompatibleSiloRelease.IsLikely(new TimeoutException()).ShouldBeFalse();
}
