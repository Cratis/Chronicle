// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Sys.for_KernelVersion.when_reading;

public class and_it_has_build_metadata : Specification
{
    SemanticVersion _version;

    void Because() => _version = KernelVersion.Parse("19.100.0-preview.2+abc123");

    [Fact] void should_preserve_the_release_version() => _version.ShouldEqual(new SemanticVersion(19, 100, 0, "preview.2"));
    [Fact] void should_read_the_commit_separately() => KernelVersion.ParseCommitSha("19.100.0-preview.2+abc123").ShouldEqual("abc123");
}
