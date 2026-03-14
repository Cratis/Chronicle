// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.given;

/// <summary>
/// Base context that sets up a temporary user home directory for specs that need config file isolation.
/// </summary>
public class a_temp_config_directory : Specification, IDisposable
{
    protected string _tempConfigHome;
    string? _previousHome;
    string? _previousUserProfile;
    string? _previousXdgConfigHome;

    void Establish()
    {
        _tempConfigHome = Path.Combine(Path.GetTempPath(), $"cratis-cli-specs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempConfigHome);

        _previousHome = Environment.GetEnvironmentVariable("HOME");
        _previousUserProfile = Environment.GetEnvironmentVariable("USERPROFILE");
        _previousXdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");

        Environment.SetEnvironmentVariable("HOME", _tempConfigHome);
        Environment.SetEnvironmentVariable("USERPROFILE", _tempConfigHome);
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", null);
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
        Environment.SetEnvironmentVariable("HOME", _previousHome);
        Environment.SetEnvironmentVariable("USERPROFILE", _previousUserProfile);
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _previousXdgConfigHome);

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
