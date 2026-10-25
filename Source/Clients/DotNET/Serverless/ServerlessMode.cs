// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// Defines whether the client runs in serverless mode.
/// </summary>
public enum ServerlessMode
{
    /// <summary>
    /// The client is a regular long-running client. This is the default.
    /// </summary>
    Off = 0,

    /// <summary>
    /// The client runs in a short-lived, scaled-to-zero host such as a function. It keeps no connection alive and
    /// opens no observer streams.
    /// </summary>
    Serverless = 1
}
