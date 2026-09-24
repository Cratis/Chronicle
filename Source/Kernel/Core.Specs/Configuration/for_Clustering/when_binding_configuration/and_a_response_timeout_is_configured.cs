// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration.for_Clustering.when_binding_configuration;

/// <summary>
/// The response timeout is the way out of a server that has grown too large to start: startup work
/// that fans out across everything the server holds needs more time as the deployment grows, while
/// Orleans' 30 second default does not move. An operator has to be able to raise it from
/// configuration alone, because by the time it is needed the server is not running.
/// </summary>
public class and_a_response_timeout_is_configured : Specification
{
    ChronicleOptions _options;

    void Establish()
    {
        _options = new ChronicleOptions();
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cratis:Chronicle:Clustering:ResponseTimeout"] = "00:05:00"
            })
            .Build()
            .GetSection(ChronicleOptions.SectionPath)
            .Bind(_options);
    }

    [Fact] void should_bind_the_configured_response_timeout() => _options.Clustering.ResponseTimeout.ShouldEqual(TimeSpan.FromMinutes(5));
}
