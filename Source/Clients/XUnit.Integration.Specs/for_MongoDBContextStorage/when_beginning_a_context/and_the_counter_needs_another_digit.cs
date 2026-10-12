// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_MongoDBContextStorage.when_beginning_a_context;

public class and_the_counter_needs_another_digit : Specification
{
    MongoDBContextStorage _storage;
    HashSet<string> _previousPrefixes;
    string _prefix;

    void Establish()
    {
        _storage = new(_ => throw new Exception("No reader should be created"));
        _previousPrefixes = Enumerable.Range(0, 15).Select(_ => _storage.BeginContext()).ToHashSet();
    }

    void Because() => _prefix = _storage.BeginContext();

    void Destroy() => _storage.ResetReaders();

    [Fact] void should_not_reuse_any_previous_prefix() => _previousPrefixes.ShouldNotContain(_prefix);
    [Fact] void should_advance_the_counter_without_padding() => _prefix.ShouldEqual("c10_");
}
