// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Defines what the server does at startup when encryption keys stored under earlier names still need the key upgrade.
/// </summary>
public enum LegacyKeyHandling
{
    /// <summary>
    /// Log a warning with the counts and start.
    /// </summary>
    Warn = 0,

    /// <summary>
    /// Inspect the whole key store and refuse to start while any key stored under an earlier name still needs the key upgrade.
    /// </summary>
    Refuse = 1
}
