// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_CompositeKeyEncoding.when_checking_legacy_ambiguity;

public class and_a_part_contains_an_underscore : Specification
{
    bool _result;

    void Because() => _result = CompositeKeyEncoding.IsLegacyIdAmbiguous(new Dictionary<string, object?> { ["a"] = "plain", ["b"] = "x_y" });

    [Fact] void should_be_ambiguous() => _result.ShouldBeTrue();
}
