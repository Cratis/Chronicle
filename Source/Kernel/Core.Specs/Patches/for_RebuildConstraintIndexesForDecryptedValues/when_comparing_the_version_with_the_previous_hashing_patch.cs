// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Patches;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RebuildConstraintIndexesForDecryptedValues;

public class when_comparing_the_version_with_the_previous_hashing_patch : given.a_rebuild_constraint_indexes_for_decrypted_values_patch
{
    RebuildConstraintIndexes _previousPatch;

    void Establish() =>
        _previousPatch = new RebuildConstraintIndexes(_storage, _grainFactory, Substitute.For<ILogger<RebuildConstraintIndexes>>());

    [Fact] void should_apply_after_the_previous_hashing_patch() => (_patch.Version > _previousPatch.Version).ShouldBeTrue();
    [Fact] void should_have_the_expected_version() => _patch.Version.ShouldEqual(new SemanticVersion(16, 4, 1));
}
