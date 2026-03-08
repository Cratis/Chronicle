// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Concepts.Patching.for_Patch;

public class when_creating_patch : Specification
{
    Patch _patch;
    SemanticVersion _version;
    DateTimeOffset _appliedAt;

    void Establish()
    {
        _version = new SemanticVersion(1, 2, 3);
        _appliedAt = DateTimeOffset.UtcNow;
    }

    void Because() => _patch = new Patch("TestPatch", _version, _appliedAt);

    [Fact] void should_have_correct_name() => _patch.Name.ShouldEqual("TestPatch");
    [Fact] void should_have_correct_version() => _patch.Version.ShouldEqual(_version);
    [Fact] void should_have_correct_applied_at() => _patch.AppliedAt.ShouldEqual(_appliedAt);
}
