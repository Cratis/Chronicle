// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Exception that is thrown when the client is incompatible with the server.
/// </summary>
/// <param name="message">The error message.</param>
public class IncompatibleServerException(string message) : Exception(message);
