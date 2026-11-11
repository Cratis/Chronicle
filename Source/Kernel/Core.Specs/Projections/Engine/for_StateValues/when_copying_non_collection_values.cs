// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_StateValues;

public class when_copying_non_collection_values : Specification
{
    object _boxedInteger;
    object _guid;
    object _date;
    object _integerResult;
    object _guidResult;
    object _dateResult;
    byte[] _bytes;
    object _bytesResult;

    void Establish()
    {
        _boxedInteger = 42;
        _guid = Guid.NewGuid();
        _date = DateTimeOffset.UtcNow;
        _bytes = [1, 2, 3];
    }

    void Because()
    {
        _integerResult = StateValues.Copy(_boxedInteger);
        _guidResult = StateValues.Copy(_guid);
        _dateResult = StateValues.Copy(_date);
        _bytesResult = StateValues.Copy(_bytes);
    }

    [Fact] void should_return_the_integer() => _integerResult.ShouldEqual(_boxedInteger);
    [Fact] void should_return_the_guid() => _guidResult.ShouldEqual(_guid);
    [Fact] void should_return_the_date() => _dateResult.ShouldEqual(_date);
    [Fact] void should_keep_the_byte_content() => ((IEnumerable<byte>)_bytesResult).SequenceEqual(_bytes).ShouldBeTrue();
    [Fact] void should_return_another_byte_collection() => _bytesResult.ShouldNotBeSame(_bytes);
}
