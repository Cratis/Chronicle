// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Sinks;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering_a_read_model_again;

public class given_named_sinks : given.a_read_models_manager
{
    protected virtual SinksOptions Sinks => new()
    {
        Configurations = new Dictionary<string, SinkConfiguration>(StringComparer.OrdinalIgnoreCase)
        {
            ["reporting"] = new() { Type = "SQL", ConnectionDetails = "Host=pg;Database=reporting" }
        }
    };

    protected readonly SwitchableOptions _options = new();

    protected override ISinkConfigurations SinkConfigurations
    {
        get
        {
            _options.Value = new ChronicleOptions { Sinks = Sinks };
            return new SinkConfigurations(_options);
        }
    }

    protected static ReadModelDefinition Orders(SinkName? name = null) =>
        DefinitionFor("orders", "Orders") with { Sink = new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB) { Name = name! } };

    protected sealed class SwitchableOptions : Microsoft.Extensions.Options.IOptions<ChronicleOptions>
    {
        public ChronicleOptions Value { get; set; } = new();
    }
}
