// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.AspNetCore.Builder;

namespace Cratis.Chronicle.for_ChronicleClientWebApplicationBuilderExtensions;

public class when_no_connection_string_is_configured : given.a_web_builder
{
    void Establish() => _builder.Configuration["ConnectionStrings:chronicle"] = null;

    void Because()
    {
        _builder.AddCratisChronicle(null);
        ResolveOptions();
    }

    [Fact] void should_keep_the_development_default() => _options.ConnectionString.ShouldBeSame(ChronicleConnectionString.Development);
    [Fact] void should_keep_the_base_options_default() => _baseOptions.ConnectionString.ShouldBeSame(ChronicleConnectionString.Development);
}
