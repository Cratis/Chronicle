// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintKeyedEncoder.when_encoding;

public class and_the_caller_mutates_the_key_buffer : Specification
{
    string _before;
    string _after;
    string _expected;

    void Because()
    {
        var key = Encoding.ASCII.GetBytes("slice-1-reference-key");
        var encoder = new UniqueConstraintKeyedEncoder(key);
        _expected = UniqueConstraintEncoder.Encode(Encoding.ASCII.GetBytes("slice-1-reference-key"), [new("x")]).AsT0.Value;
        _before = encoder.EncodeValue([new("x")]).AsT0.Value;
        Array.Clear(key);
        _after = encoder.EncodeValue([new("x")]).AsT0.Value;
    }

    [Fact] void should_have_used_the_original_key() => _before.ShouldEqual(_expected);
    [Fact] void should_keep_using_the_copied_key() => _after.ShouldEqual(_expected);
}
