// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Configuration.for_ConnectionFiles.given;

public class connection_files : Specification
{
    protected string _directory;
    protected Dictionary<string, string?> _settings;
    protected IConfiguration _configuration;
    ServiceProvider? _services;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"chronicle-connection-files-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        _settings = [];
    }

    void Destroy()
    {
        _services?.Dispose();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    protected string WriteFile(string name, string content)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, content);
        return path;
    }

    protected void Setting(string key, string? value) => _settings[$"{ChronicleOptions.SectionPath}:{key}"] = value;

    protected void AddConfiguration()
    {
        _settings.TryAdd($"{ChronicleOptions.SectionPath}:Storage:Type", "inmemory");
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(_settings).Build();
        var services = new ServiceCollection();
        services.AddSingleton(_configuration);
        ChronicleOptions.AddConfiguration(services, _configuration);
        _services = services.BuildServiceProvider();
    }

    protected ChronicleOptions Options()
    {
        _services!.GetRequiredService<IStartupValidator>().Validate();
        return _services!.GetRequiredService<IOptions<ChronicleOptions>>().Value;
    }
}
