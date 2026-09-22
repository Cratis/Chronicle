// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration.for_Clustering.when_binding_configuration;

/// <summary>
/// Left alone, the response timeout stays at the value Orleans itself uses. A call that hangs
/// should still fail rather than tie its caller up indefinitely, so the larger budget is something
/// an operator opts into for a specific deployment rather than something everyone silently gets.
/// </summary>
public class and_nothing_is_configured : Specification
{
    ChronicleOptions _options;

    void Establish()
    {
        _options = new ChronicleOptions();
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build()
            .GetSection(ChronicleOptions.SectionPath)
            .Bind(_options);
    }

    [Fact] void should_default_to_the_orleans_response_timeout() => _options.Clustering.ResponseTimeout.ShouldEqual(TimeSpan.FromSeconds(30));
}
