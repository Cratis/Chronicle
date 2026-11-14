// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Diagnostics.Identifiers.given;

public class a_logger_with_identifier_redaction : Specification
{
    protected RecordingIdentifierLoggerProvider _provider;
    protected ILogger<Observer> _logger;
    ILoggerFactory _factory;

    protected virtual Telemetry Telemetry => new();

    void Establish()
    {
        _provider = new RecordingIdentifierLoggerProvider();
        _factory = LoggerFactory.Create(builder =>
        {
            builder.Services.AddSingleton<IOptions<ChronicleOptions>>(Options.Create(new ChronicleOptions { Telemetry = Telemetry }));
            builder.EnableRedaction();
            builder.AddChronicleLogRedaction();
            builder.AddProvider(_provider);
        });
        _logger = _factory.CreateLogger<Observer>();
    }

    void Destroy() => _factory.Dispose();
}
