// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Serverless.for_ChronicleServerlessOptions;

public class when_binding_delivery_limits : Specification
{
    ChronicleServerlessOptions _options;
    void Because()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Chronicle:Serverless:Delivery:MaximumBodySize"] = "1024"
        }).Build();
        using var lifetime = configuration as IDisposable;
        _options = configuration.GetSection("Cratis:Chronicle:Serverless").Get<ChronicleServerlessOptions>();
    }
    [Fact] void should_not_bind_the_internal_delivery_limit() => _options.Delivery.MaximumBodySize.ShouldEqual(16L * 1024 * 1024);
}
