// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Setup.for_GrainDirectoryConfiguration.given;

public class a_silo_builder : Specification
{
    protected ServiceCollection _services;
    protected ISiloBuilder _builder;

    void Establish()
    {
        _services = new ServiceCollection();
        _builder = Substitute.For<ISiloBuilder>();
        _builder.Services.Returns(_services);
    }

    protected void Configure(Dictionary<string, string?> settings) => _builder.Configuration.Returns(
        new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
}
