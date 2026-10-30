// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Exception that gets thrown when a client-hosted observer is registered while the client skips the keep-alive connection.
/// </summary>
/// <param name="observerKind">The kind of observer, such as Reactor or Reducer.</param>
/// <param name="observerId">The identifier of the observer.</param>
public class ClientObserversRequireKeepAlive(string observerKind, string observerId)
    : Exception($"{observerKind} '{observerId}' cannot be registered because this client skips the keep-alive connection (short-lived process). Client-hosted observers need a long-running process: host them in a service that does not skip keep-alive.");
