// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.DevelopmentTools;

/// <summary>
/// The exception that is thrown when a development tool is used on a server that was not built with them.
/// </summary>
public class DevelopmentToolsNotAvailable()
    : Exception("Development tools are only available when the server is compiled with the DEVELOPMENT preprocessor symbol.");
