// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.for_ChronicleHostApplicationBuilderExtensions;

public class when_the_callback_selects_development_over_the_named_connection_string : given.a_host_builder
{
    void Establish() => _builder.Configuration["ConnectionStrings:chronicle"] = "invalid";

    void Because()
    {
        _builder.AddCratisChronicle(options => options.ConnectionString = ChronicleConnectionString.Development);
        ResolveOptions();
    }

    [Fact] void should_keep_the_explicit_development_connection_string() => _options.ConnectionString.ShouldBeSame(ChronicleConnectionString.Development);
}
