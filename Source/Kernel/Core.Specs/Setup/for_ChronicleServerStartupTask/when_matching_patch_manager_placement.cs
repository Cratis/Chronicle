// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask;

public class when_matching_patch_manager_placement : Specification
{
    [Fact] void should_match_the_orleans_patch_manager_placement_failure() => ChronicleServerStartupTask.IsPatchManagerAwaitingManifest(new OrleansException("No active nodes are compatible with grain patchmanager and interface Cratis.Chronicle.Patching.IPatchManager version 2. Known nodes with grain type: none. All known nodes compatible with interface version: none")).ShouldBeTrue();
    [Fact] void should_reject_a_different_grain_type() => ChronicleServerStartupTask.IsPatchManagerAwaitingManifest(new OrleansException("No active nodes are compatible with grain observer and interface Cratis.Chronicle.Patching.IPatchManager version 2.")).ShouldBeFalse();
    [Fact] void should_reject_unrelated_orleans_errors() => ChronicleServerStartupTask.IsPatchManagerAwaitingManifest(new OrleansException("placement failed")).ShouldBeFalse();
}
