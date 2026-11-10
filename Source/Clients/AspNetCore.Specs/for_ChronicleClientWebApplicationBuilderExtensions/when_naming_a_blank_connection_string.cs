// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;

namespace Cratis.Chronicle.for_ChronicleClientWebApplicationBuilderExtensions;

public class when_naming_a_blank_connection_string : given.a_web_builder
{
    Exception _empty;
    Exception _whitespace;

    void Because()
    {
        _empty = Catch.Exception(() => _builder.AddCratisChronicleUsingConnectionString(string.Empty));
        _whitespace = Catch.Exception(() => _builder.AddCratisChronicleUsingConnectionString("  "));
    }

    [Fact] void should_reject_an_empty_name() => _empty.ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_reject_a_whitespace_name() => _whitespace.ShouldBeOfExactType<ArgumentException>();
}
