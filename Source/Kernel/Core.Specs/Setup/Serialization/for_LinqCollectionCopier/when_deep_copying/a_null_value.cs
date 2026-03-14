// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Serialization.for_LinqCollectionCopier.when_deep_copying;

public class a_null_value : Specification
{
    LinqCollectionCopier _copier;
    object? _result;

    void Establish() => _copier = new LinqCollectionCopier();

    void Because() => _result = _copier.DeepCopy(null, null!);

    [Fact] void should_return_null() => _result.ShouldBeNull();
}
