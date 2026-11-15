// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;
using ChroniclePatch = Cratis.Chronicle.Concepts.Patching.Patch;
using ChronicleVersion = Cratis.Chronicle.Concepts.System.SemanticVersion;

namespace Cratis.Chronicle.Patching.for_ChroniclePatchStorage.when_reading;

public class applied_patches : given.a_patch_storage
{
    static readonly DateTimeOffset _appliedAt = new(2026, 5, 1, 8, 30, 0, TimeSpan.Zero);
    AppliedPatch[] _result;

    void Establish() => _patches.GetAll().Returns(Task.FromResult<IEnumerable<ChroniclePatch>>([new ChroniclePatch("rename-reactors", new ChronicleVersion(15, 2, 4), _appliedAt)]));

    async Task Because() => _result = [.. await _storage.GetAll()];

    [Fact] void should_have_the_patch_the_store_recorded() => _result.Select(_ => _.Name).ShouldEqual(["rename-reactors"]);
    [Fact] void should_keep_its_version() => _result[0].Version.ShouldEqual(new SemanticVersion(15, 2, 4));
    [Fact] void should_keep_when_it_was_applied() => _result[0].AppliedAt.ShouldEqual(_appliedAt);
}
