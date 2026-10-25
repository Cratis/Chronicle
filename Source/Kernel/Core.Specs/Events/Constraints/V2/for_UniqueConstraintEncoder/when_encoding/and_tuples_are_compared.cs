// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintEncoder.when_encoding;

public class and_tuples_are_compared : Specification
{
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-1-reference-key");
    Dictionary<string, string> _values;
    string _repeated;
    string _otherKey;
    string _otherCulture;

    void Because()
    {
        var inputs = new Dictionary<string, UniqueConstraintComponent[]>
        {
            ["left-dash"] = [new("a-b"), new("c")],
            ["right-dash"] = [new("a"), new("b-c")],
            ["left-colon"] = [new("a:b"), new("c")],
            ["right-colon"] = [new("a"), new("b:c")],
            ["integer"] = [new(1, "int32")],
            ["string"] = [new("1")],
            ["composed"] = [new("é")],
            ["decomposed"] = [new("e\u0301")],
            ["tuple"] = [new("I"), new(1.25d), new("😀")],
            ["reversed"] = [new("😀"), new(1.25d), new("I")]
        };
        _values = inputs.ToDictionary(_ => _.Key, _ => UniqueConstraintEncoder.Encode(_key, _.Value).AsT0.Value);
        _repeated = UniqueConstraintEncoder.Encode(_key, inputs["tuple"]).AsT0.Value;
        _otherKey = UniqueConstraintEncoder.Encode(Encoding.ASCII.GetBytes("a-different-fixture-key"), inputs["tuple"]).AsT0.Value;
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new("fr-FR");
            _otherCulture = UniqueConstraintEncoder.Encode(_key, inputs["tuple"]).AsT0.Value;
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact] void should_not_alias_dashes_inside_values() => _values["left-dash"].ShouldNotEqual(_values["right-dash"]);
    [Fact] void should_not_alias_colons_inside_values() => _values["left-colon"].ShouldNotEqual(_values["right-colon"]);
    [Fact] void should_not_write_a_kind_byte() => _values["integer"].ShouldEqual(_values["string"]);
    [Fact] void should_not_normalize_unicode() => _values["composed"].ShouldNotEqual(_values["decomposed"]);
    [Fact] void should_preserve_declaration_order() => _values["tuple"].ShouldNotEqual(_values["reversed"]);
    [Fact] void should_be_deterministic() => _repeated.ShouldEqual(_values["tuple"]);
    [Fact] void should_be_keyed() => _otherKey.ShouldNotEqual(_values["tuple"]);
    [Fact] void should_be_culture_independent() => _otherCulture.ShouldEqual(_values["tuple"]);
    [Fact] void should_use_version_two() => UniqueConstraintEncoder.Version.Value.ShouldEqual(2);
    [Fact] void should_return_a_full_sha256_digest() => _values["tuple"].Length.ShouldEqual(64);
    [Fact] void should_return_lowercase_hex() => _values["tuple"].All(_ => _ is >= '0' and <= '9' or >= 'a' and <= 'f').ShouldBeTrue();
}
