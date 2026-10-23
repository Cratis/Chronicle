// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_CompositeKeyEncoding.when_checking_legacy_ambiguity;

public class and_all_parts_are_plain : Specification
{
    bool _result;

    void Because() => _result = CompositeKeyEncoding.IsLegacyIdAmbiguous(new Dictionary<string, object?> { ["a"] = "plain", ["b"] = 42, ["c"] = Guid.NewGuid(), ["d"] = "x:y~z" });

    [Fact] void should_not_be_ambiguous() => _result.ShouldBeFalse();
}
