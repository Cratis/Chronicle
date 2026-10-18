// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.AspNetCore.Builder;

namespace Cratis.Chronicle.for_ChronicleClientWebApplicationBuilderExtensions;

public class when_the_callback_selects_development_over_the_named_connection_string : given.a_web_builder
{
    void Establish() => _builder.Configuration["ConnectionStrings:chronicle"] = "invalid";

    void Because()
    {
        _builder.AddCratisChronicle(options => options.ConnectionString = ChronicleConnectionString.Development);
        ResolveOptions();
    }

    [Fact] void should_keep_the_explicit_development_connection_string() => _options.ConnectionString.ShouldBeSame(ChronicleConnectionString.Development);
    [Fact] void should_configure_the_base_options_connection_string() => _baseOptions.ConnectionString.ShouldBeSame(ChronicleConnectionString.Development);
}
