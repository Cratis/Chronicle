// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.given;

/// <summary>
/// Base context that sets up a temporary XDG config directory for specs that need config file isolation.
/// </summary>
public class a_temp_config_directory : Specification, IDisposable
{
    protected string _tempConfigHome;

    void Establish()
    {
        _tempConfigHome = Path.Combine(Path.GetTempPath(), $"cratis-cli-specs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempConfigHome);
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _tempConfigHome);
    }

    /// <inheritdoc/>
#pragma warning disable CA1033 // Interface methods should be callable by child types — CleanUp() serves this purpose
    void IDisposable.Dispose()
#pragma warning restore CA1033
    {
        CleanUp();
    }

    /// <summary>
    /// Cleans up the temporary directory and resets the environment variable.
    /// </summary>
    protected virtual void CleanUp()
    {
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", null);

        try
        {
            if (Directory.Exists(_tempConfigHome))
            {
                Directory.Delete(_tempConfigHome, true);
            }
        }
        catch
        {
            // Best-effort cleanup.
        }
    }
}
