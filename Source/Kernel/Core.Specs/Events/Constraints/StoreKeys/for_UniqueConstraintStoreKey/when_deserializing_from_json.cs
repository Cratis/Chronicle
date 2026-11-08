// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.StoreKeys.for_UniqueConstraintStoreKey;

public class when_deserializing_from_json : Specification
{
    Exception? _valueError;
    Exception? _nullError;

    void Because()
    {
        _valueError = Catch.Exception(() => _ = JsonSerializer.Deserialize<UniqueConstraintStoreKey>("\"synthetic-material-input\""));
        _nullError = Catch.Exception(() => _ = JsonSerializer.Deserialize<UniqueConstraintStoreKey>("null"));
    }

    [Fact] void should_reject_json_values_explicitly() => _valueError.ShouldBeOfExactType<NotSupportedException>();
    [Fact] void should_reject_json_null_explicitly() => _nullError.ShouldBeOfExactType<NotSupportedException>();
    [Fact] void should_report_the_explicit_policy() => _valueError!.Message.StartsWith("Unique-constraint store key material cannot be serialized or deserialized as JSON.", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_include_the_supplied_value_in_diagnostics() => _valueError!.ToString().Contains("synthetic-material-input", StringComparison.Ordinal).ShouldBeFalse();
}
