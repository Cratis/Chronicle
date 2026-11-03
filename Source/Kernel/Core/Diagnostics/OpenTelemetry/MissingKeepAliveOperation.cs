// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry;

/// <summary>
/// The exception that is thrown when the wire contract does not contain the connection keep-alive operation.
/// </summary>
public class MissingKeepAliveOperation() : Exception("The wire contract is missing the connection keep-alive operation.");
