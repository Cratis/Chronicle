// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Setup.for_KernelCommandExtensionPoints.given;

/// <summary>
/// Stands in for an application's own rule for reading the key of a command, which claims every command it is asked about.
/// </summary>
public class ApplicationCommandKeyResolver : ICanResolveKeyForCommand
{
    public const string Key = "application-key";

    public string? Resolve(object command)
    {
        if (ApplicationExtensionPoints.Recorder is not { } recorder)
        {
            return null;
        }

        recorder.KeysResolved++;
        return Key;
    }
}
