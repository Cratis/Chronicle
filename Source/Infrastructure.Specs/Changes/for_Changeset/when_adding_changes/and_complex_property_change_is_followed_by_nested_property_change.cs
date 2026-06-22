// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_changes;

public class and_complex_property_change_is_followed_by_nested_property_change : given.a_changeset
{
    const string PlainDisplayName = "Ada Lovelace";
    const string EncryptedDisplayName = "encrypted-display-name";

    PropertyDifference[] _differences;
    IDictionary<string, object?> _displayName;

    void Establish()
    {
        var plainDisplayName = Expando(
            ("name", PlainDisplayName),
            ("verificationLevel", 1));
        var encryptedDisplayName = Expando(
            ("name", EncryptedDisplayName),
            ("verificationLevel", 1));

        var projectedState = Expando(("displayName", plainDisplayName));
        var encryptedState = Expando(("displayName", encryptedDisplayName));

        _changeset.Add(new PropertiesChanged<ExpandoObject>(
            projectedState,
            [new PropertyDifference("displayName", null, plainDisplayName)]));

        _changeset.Add(new PropertiesChanged<ExpandoObject>(
            encryptedState,
            [new PropertyDifference("displayName.name", PlainDisplayName, EncryptedDisplayName)]));
    }

    void Because()
    {
        _differences = _changeset.Changes
            .OfType<PropertiesChanged<ExpandoObject>>()
            .Single()
            .Differences
            .ToArray();

        _displayName = (IDictionary<string, object?>)_differences.Single().Changed!;
    }

    [Fact] void should_keep_one_properties_changed() => _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().Count().ShouldEqual(1);
    [Fact] void should_keep_one_difference() => _differences.Length.ShouldEqual(1);
    [Fact] void should_keep_the_complex_property_change() => _differences.Single().PropertyPath.ShouldEqual(new PropertyPath("displayName"));
    [Fact] void should_apply_the_nested_change_to_the_complex_value() => _displayName["name"].ShouldEqual(EncryptedDisplayName);
    [Fact] void should_keep_the_other_complex_value_members() => _displayName["verificationLevel"].ShouldEqual(1);

    static ExpandoObject Expando(params (string Key, object? Value)[] properties)
    {
        var result = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)result;
        foreach (var (key, value) in properties)
        {
            dictionary[key] = value;
        }

        return result;
    }
}
