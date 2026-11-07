// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Releases.for_ClusterReleaseBoundary;

public class when_every_release_is_after_the_rename : Specification
{
    IReadOnlyList<SemanticVersion> _result;

    void Because() => _result = ClusterReleaseBoundary.FindIncompatible(
        new SemanticVersion(19, 124, 0),
        new ClusterReleaseSummary([(new SemanticVersion(19, 123, 1), 1), (new SemanticVersion(19, 124, 0), 1)], 0, 0));

    [Fact] void should_report_nothing() => _result.ShouldBeEmpty();
}
