// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Connections;

/// <summary>
/// Represents the information sent to the Kernel when connecting.
/// </summary>
/// <param name="ClientVersion">The version of the client.</param>
/// <param name="IsRunningWithDebugger">Whether or not the client is running with debugger attached.</param>
public record ClientInformation(string ClientVersion, bool IsRunningWithDebugger);
