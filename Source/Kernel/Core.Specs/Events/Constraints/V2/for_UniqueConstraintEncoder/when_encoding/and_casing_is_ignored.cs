// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintEncoder.when_encoding;

public class and_casing_is_ignored : Specification
{
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-1-reference-key");
    string _ignored;
    string _lowercase;
    string _sensitive;
    string _number;
    string _numberSensitive;
    string _unknown;
    string _unknownLowercase;
    string _unknownGuid;
    string _schemaGuid;

    void Because()
    {
        _ignored = Value([new("I"), new(new string?[] { "ABC", null, "İ" })], true);
        _lowercase = Value([new("i"), new(new string?[] { "abc", null, "İ".ToLowerInvariant() })], false);
        _sensitive = Value([new("I"), new(new string?[] { "ABC", null, "İ" })], false);
        _number = Value([new(1E+100d, "double"), new(new[] { true, false }), new(new[] { 1E+20f }, "float")], true);
        _numberSensitive = Value([new(1E+100d, "double"), new(new[] { true, false }), new(new[] { 1E+20f }, "float")], false);
        _unknown = Value([new("I-NOT-A-GUID", IsInSchema: false), new(new[] { "ABC" }, IsInSchema: false)], true);
        _unknownLowercase = Value([new("i-not-a-guid", IsInSchema: false), new(new[] { "abc" }, IsInSchema: false)], false);
        _unknownGuid = Value([new("{ABCDEF01-2345-6789-ABCD-EF0123456789}", IsInSchema: false)], true);
        _schemaGuid = Value([new(new Guid("abcdef01-2345-6789-abcd-ef0123456789"), "guid")], false);
    }

    string Value(UniqueConstraintComponent[] components, bool ignoreCasing) => UniqueConstraintEncoder.Encode(_key, components, ignoreCasing).AsT0.Value;

    [Fact] void should_lowercase_scalar_and_array_string_leaves_invariantly() => _ignored.ShouldEqual(_lowercase);
    [Fact] void should_preserve_case_by_default() => _sensitive.ShouldNotEqual(_lowercase);
    [Fact] void should_not_lowercase_numeric_exponents_or_change_booleans() => _number.ShouldEqual(_numberSensitive);
    [Fact] void should_lowercase_unknown_non_guid_strings() => _unknown.ShouldEqual(_unknownLowercase);
    [Fact] void should_normalize_unknown_guid_strings_before_framing() => _unknownGuid.ShouldEqual(_schemaGuid);
}
