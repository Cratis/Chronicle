// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.for_ChronicleHostApplicationBuilderExtensions;

public class when_the_named_connection_string_is_empty : given.a_host_builder
{
    void Establish() => _builder.Configuration["ConnectionStrings:chronicle"] = " ";

    void Because()
    {
        _builder.AddCratisChronicle(default);
        ResolveOptions();
    }

    [Fact] void should_keep_the_development_default() => _options.ConnectionString.ShouldBeSame(ChronicleConnectionString.Development);
}
