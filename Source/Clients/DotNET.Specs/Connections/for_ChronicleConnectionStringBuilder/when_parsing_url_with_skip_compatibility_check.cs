// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_parsing_url_with_skip_compatibility_check : Specification
{
    ChronicleConnectionStringBuilder _builder;

    void Establish() => _builder = new ChronicleConnectionStringBuilder("chronicle://localhost:35000/?skipCompatibilityCheck=true");

    [Fact] void should_have_skip_compatibility_check_set_to_true() => _builder.SkipCompatibilityCheck.ShouldBeTrue();
}
