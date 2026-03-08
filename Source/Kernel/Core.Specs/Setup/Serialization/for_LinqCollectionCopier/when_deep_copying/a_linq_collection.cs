// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Serialization.Cloning;

namespace Cratis.Chronicle.Setup.Serialization.for_LinqCollectionCopier.when_deep_copying;

public class a_linq_collection : Specification
{
    LinqCollectionCopier _copier;
    IEnumerable<int> _linqCollection;
    object? _result;

    void Establish()
    {
        _copier = new LinqCollectionCopier();
        _linqCollection = new[] { 1, 2, 3 }.Select(x => x);
    }

    void Because() => _result = _copier.DeepCopy(_linqCollection, null!);

    [Fact] void should_return_non_null() => _result.ShouldNotBeNull();
    [Fact] void should_return_an_array() => _result.ShouldBeOfExactType<int[]>();
    [Fact] void should_contain_all_elements() => (_result as int[]).ShouldContainOnly(1, 2, 3);
}
