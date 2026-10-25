// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Json;

namespace Cratis.Chronicle.Security.for_GrantReason;

public class when_serializing_a_management_grant : Specification
{
    GrantReason _roundTripped;

    void Because()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new EnumConverterFactory());
        var json = JsonSerializer.Serialize(GrantReason.AdministratorGrant, options);
        _roundTripped = JsonSerializer.Deserialize<GrantReason>(json, options);
    }

    [Fact] void should_reserve_the_agreed_numeric_value() => ((int)GrantReason.AdministratorGrant).ShouldEqual(3);
    [Fact] void should_replay_the_new_reason_with_the_pinned_enum_converter() => _roundTripped.ShouldEqual(GrantReason.AdministratorGrant);
}
