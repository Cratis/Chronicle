// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Serialization.for_LinqCollectionCopier.when_checking_if_type_is_supported;

public class with_linq_where_iterator : Specification
{
    LinqCollectionCopier _copier;
    Type _type;
    bool _result;

    void Establish()
    {
        _copier = new LinqCollectionCopier();
        _type = new[] { 1, 2, 3 }.Where(x => x > 1).GetType();
    }

    void Because() => _result = _copier.IsSupportedType(_type);

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
