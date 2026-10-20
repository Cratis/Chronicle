// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Patches;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RecordSubjectsOnUniqueConstraintClaims;

public class when_comparing_the_version_with_the_previous_rebuild : given.a_record_subjects_on_unique_constraint_claims_patch
{
    RebuildConstraintIndexesForDecryptedValues _previousPatch;

    void Establish() =>
        _previousPatch = new RebuildConstraintIndexesForDecryptedValues(_storage, _grainFactory, Substitute.For<ILogger<RebuildConstraintIndexesForDecryptedValues>>());

    [Fact] void should_apply_after_the_previous_rebuild() => (_patch.Version > _previousPatch.Version).ShouldBeTrue();
    [Fact] void should_have_the_expected_version() => _patch.Version.ShouldEqual(new SemanticVersion(19, 100, 0));
}
