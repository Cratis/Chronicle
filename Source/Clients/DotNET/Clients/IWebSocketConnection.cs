// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Clients;

/// <summary>
/// Defines a connection using Web Sockets.
/// </summary>
public interface IWebSocketConnection : IDisposable
{
    Task Connect();
}
