// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_ParkedKeyFailure.when_mutating_source_messages_after_creating;

public class and_the_array_is_copied : Specification
{
    string[] _source;
    ParkedKeyFailure _failure;
    ParkedKeyFailure _equal;

    void Establish()
    {
        _source = ["original"];
        _failure = new(null, ImmutableCollectionsMarshal.AsImmutableArray(_source), "stack", DateTimeOffset.UnixEpoch);
        _equal = new(null, ["original"], "stack", DateTimeOffset.UnixEpoch);
    }

    void Because() => _source[0] = "changed";

    [Fact] void should_keep_the_original_message() => _failure.Messages[0].ShouldEqual("original");
    [Fact] void should_compare_by_content() => _failure.ShouldEqual(_equal);
    [Fact] void should_hash_by_content() => _failure.GetHashCode().ShouldEqual(_equal.GetHashCode());
}
