// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_CompositeKeyNormalizer.when_normalizing;

public class and_parts_are_given_in_another_order : given.a_composite_key_schema
{
    string _inOrder;
    string _reversed;

    void Because()
    {
        var parts = ValidParts();
        _inOrder = CompositeKeyEncoding.Encode(CompositeKeyNormalizer.Normalize(_schema, parts, _typeFormats).Key!);
        _reversed = CompositeKeyEncoding.Encode(CompositeKeyNormalizer.Normalize(_schema, parts.Reverse().ToDictionary(_ => _.Key, _ => _.Value), _typeFormats).Key!);
    }

    [Fact] void should_encode_to_the_same_value() => _reversed.ShouldEqual(_inOrder);
}
