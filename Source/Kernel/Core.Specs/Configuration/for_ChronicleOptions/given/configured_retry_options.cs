// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.given;

public class configured_retry_options : Specification
{
    protected const string Key = "retry-binding#store#namespace#sequence";
    protected IConfigurationRoot _configuration;
    protected ServiceProvider _services;
    protected IOptionsMonitor<ChronicleOptions> _monitor;
    protected ConfigurationForObserverProvider _provider;
    protected Observers _result;
    Stream _json;
    readonly Dictionary<string, string?> _environment = new();

    protected void SetEnvironment(string name, string? value)
    {
        if (!_environment.ContainsKey(name)) _environment[name] = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
    }

    protected void Configure(string? json = null)
    {
        var builder = new ConfigurationBuilder();
        if (json is not null)
        {
            _json = new MemoryStream(Encoding.UTF8.GetBytes(json));
            builder.AddJsonStream(_json);
        }
        else
        {
            builder.AddInMemoryCollection();
        }
        _configuration = builder.Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(_configuration);
        ChronicleOptions.AddConfiguration(services, _configuration);
        _services = services.BuildServiceProvider();
        _services.GetRequiredService<IStartupValidator>().Validate();
        _monitor = _services.GetRequiredService<IOptionsMonitor<ChronicleOptions>>();
        _provider = new(_monitor);
    }

    protected static ValidationResult[] Validate(ObserverRetryOverrides entry, Observers? defaults = null, string key = Key) =>
        Validate(new ChronicleOptions { Observers = defaults ?? new(), ObserverRetryOverrides = new Dictionary<string, ObserverRetryOverrides> { [key] = entry } });

    protected static ValidationResult[] Validate(ChronicleOptions options) => options.Validate(new ValidationContext(options)).ToArray();

    void Destroy()
    {
        _services?.Dispose();
        _configuration?.Dispose();
        _json?.Dispose();
        foreach (var entry in _environment) Environment.SetEnvironmentVariable(entry.Key, entry.Value);
    }
}
