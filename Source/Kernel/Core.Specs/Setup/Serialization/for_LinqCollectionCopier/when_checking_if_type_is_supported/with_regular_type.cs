// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Serialization.for_LinqCollectionCopier.when_checking_if_type_is_supported;

public class with_regular_type : Specification
{
    LinqCollectionCopier _copier;
    bool _result;

    void Establish() => _copier = new LinqCollectionCopier();

    void Because() => _result = _copier.IsSupportedType(typeof(string));

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
