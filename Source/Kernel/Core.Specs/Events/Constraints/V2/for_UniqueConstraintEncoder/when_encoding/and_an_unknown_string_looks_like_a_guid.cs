// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintEncoder.when_encoding;

public class and_an_unknown_string_looks_like_a_guid : Specification
{
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-1-reference-key");
    string _upper;
    string _lower;

    void Because()
    {
        _upper = Value("ABCDEF01-2345-6789-ABCD-EF0123456789");
        _lower = Value("abcdef01-2345-6789-abcd-ef0123456789");
    }

    string Value(string text) => UniqueConstraintEncoder.Encode(_key, [new(text, IsInSchema: false)]).AsT0.Value;

    [Fact] void should_encode_upper_and_lower_case_equally() => _upper.ShouldEqual(_lower);
}
