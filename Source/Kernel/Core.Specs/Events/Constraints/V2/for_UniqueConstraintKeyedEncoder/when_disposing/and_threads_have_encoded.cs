// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintKeyedEncoder.when_disposing;

public class and_threads_have_encoded : Specification
{
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-1-reference-key");
    UniqueConstraintKeyedEncoder _encoder;
    Exception? _firstDispose;
    Exception? _secondDispose;
    Exception? _encodeAfterDispose;

    void Establish()
    {
        _encoder = new UniqueConstraintKeyedEncoder(_key);
        Parallel.For(0, 64, new ParallelOptions { MaxDegreeOfParallelism = 8 }, index =>
            _encoder.EncodeValue([new($"value-{index}")]));
    }

    void Because()
    {
        _firstDispose = Catch.Exception(_encoder.Dispose);
        _secondDispose = Catch.Exception(_encoder.Dispose);
        _encodeAfterDispose = Catch.Exception(() => _encoder.EncodeValue([new("value")]));
    }

    [Fact] void should_dispose_without_failing() => _firstDispose.ShouldBeNull();
    [Fact] void should_tolerate_being_disposed_twice() => _secondDispose.ShouldBeNull();
    [Fact] void should_not_be_usable_afterwards() => _encodeAfterDispose.ShouldBeOfExactType<ObjectDisposedException>();
}
