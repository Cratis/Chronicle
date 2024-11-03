// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Workbench.Embedded;

/// <summary>
/// Represents the options for the workbench.
/// </summary>
public class ChronicleWorkbenchOptions
{
    /// <summary>
    /// Gets the default port to expose the Workbench on.
    /// </summary>
    public const int DefaultPort = 9876;

    /// <summary>
    /// Gets or sets the port to expose the Workbench on.
    /// </summary>
    public int Port { get; set; } = DefaultPort;
}
