// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Sinks;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Generations.Bindings.for_MaterializationKeys;

public class when_the_kernel_default_sink_changes : given.a_materialization
{
    MaterializationKey _onPrimary;
    MaterializationKey _onSecondary;

    async Task Because()
    {
        var configurations = new Dictionary<string, SinkConfiguration>(StringComparer.OrdinalIgnoreCase)
        {
            ["primary"] = new() { Type = "MongoDB", ConnectionDetails = "mongodb://primary:27017" },
            ["secondary"] = new() { Type = "MongoDB", ConnectionDetails = "mongodb://secondary:27017" }
        };
        _onPrimary = await KeyWith(new() { ReadModels = "primary", Configurations = configurations });
        _onSecondary = await KeyWith(new() { ReadModels = "secondary", Configurations = configurations });
    }

    Task<MaterializationKey> KeyWith(SinksOptions sinks) =>
        new MaterializationKeys(_storage, new SinkConfigurations(Options.Create(new ChronicleOptions { Sinks = sinks }))).For(_name, _namespace, _projection);

    [Fact] void should_be_another_materialization() => _onSecondary.Value.ShouldNotEqual(_onPrimary.Value);
}
