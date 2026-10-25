// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.for_ChronicleOptions.when_binding_from_configuration;

public class with_short_lived_settings : Specification
{
    ChronicleOptions _result;

    void Because()
    {
        var configuration = new Dictionary<string, string?>
        {
            ["Chronicle:SkipKeepAlive"] = "true",
            ["Chronicle:RegistrationMode"] = "AtDeployment"
        };

        var configurationRoot = new ConfigurationBuilder()
            .AddInMemoryCollection(configuration)
            .Build();

        _result = new ChronicleOptions();
        configurationRoot.GetSection("Chronicle").Bind(_result);
    }

    [Fact] void should_bind_skip_keep_alive() => _result.SkipKeepAlive.ShouldBeTrue();
    [Fact] void should_bind_registration_mode() => _result.RegistrationMode.ShouldEqual(RegistrationMode.AtDeployment);
}
