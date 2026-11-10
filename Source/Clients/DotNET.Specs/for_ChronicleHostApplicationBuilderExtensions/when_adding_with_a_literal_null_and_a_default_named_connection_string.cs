// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.for_ChronicleHostApplicationBuilderExtensions;

public class when_adding_with_a_literal_null_and_a_default_named_connection_string : given.a_host_builder
{
    void Establish() => _builder.Configuration["ConnectionStrings:chronicle"] = "chronicle://defaulthost:4321";

    void Because()
    {
        _builder.AddCratisChronicle(null);
        ResolveOptions();
    }

    [Fact] void should_still_use_the_chronicle_connection_string() => _options.ConnectionString.ServerAddress.Host.ShouldEqual("defaulthost");
}
