// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.given;

namespace Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.when_something_was_added;

/// <summary>
/// An older peer does not use what it does not know about, so additions can never break it. Reporting them would
/// make the gate fire on every ordinary feature and teach everyone to ignore it.
/// </summary>
public class and_the_newer_contract_only_grew : Specification
{
    WireContract _expected;
    WireContract _actual;
    WireCompatibilityReport _result;

    void Establish()
    {
        _expected = WireContracts.With();

        var grown = WireContracts.With();
        _actual = new WireContract(
            grown.Services.Concat(new Dictionary<string, WireService> { [".test.More"] = new(".test.More", new Dictionary<string, WireMethod>(StringComparer.Ordinal)) }).ToDictionary(_ => _.Key, _ => _.Value, StringComparer.Ordinal),
            grown.Messages.ToDictionary(
                _ => _.Key,
                _ => new WireMessage(_.Value.FullName, _.Value.Fields.Append(new(2, new WireField(2, "Added", "int32", WireFieldLabel.Singular, null))).ToDictionary(f => f.Key, f => f.Value)),
                StringComparer.Ordinal),
            grown.Enums.ToDictionary(
                _ => _.Key,
                _ => new WireEnum(_.Value.FullName, _.Value.Values.Append(new(1, "Blue")).ToDictionary(v => v.Key, v => v.Value)),
                StringComparer.Ordinal));
    }

    void Because() => _result = WireCompatibilityChecker.Check(_expected, _actual);

    [Fact] void should_report_compatible() => _result.IsCompatible.ShouldBeTrue();
    [Fact] void should_report_nothing() => _result.Incompatibilities.ShouldBeEmpty();
}
