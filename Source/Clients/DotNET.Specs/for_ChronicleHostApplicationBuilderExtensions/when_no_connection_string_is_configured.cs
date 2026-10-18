// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.for_ChronicleHostApplicationBuilderExtensions;

public class when_no_connection_string_is_configured : given.a_host_builder
{
    void Establish() => _builder.Configuration["ConnectionStrings:chronicle"] = null;

    void Because()
    {
        _builder.AddCratisChronicle();
        ResolveOptions();
    }

    [Fact] void should_keep_the_development_default() => _options.ConnectionString.ShouldBeSame(ChronicleConnectionString.Development);
}
