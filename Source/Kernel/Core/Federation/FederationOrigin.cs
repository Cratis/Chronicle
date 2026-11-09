// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Where a federation binding or grant came from.
/// </summary>
public static class FederationOrigin
{
    /// <summary>
    /// Created or changed at runtime through a command.
    /// </summary>
    public const string Runtime = "Runtime";

    /// <summary>
    /// Bootstrapped from configuration.
    /// </summary>
    public const string Configuration = "Configuration";
}
