// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.given;

/// <summary>
/// An HTTP handler that wraps the one a channel creates and remembers whether the channel has disposed it.
/// </summary>
/// <param name="inner">The handler the channel would otherwise have used.</param>
public class a_tracking_http_handler(HttpMessageHandler inner) : DelegatingHandler(inner)
{
    /// <summary>Gets a value indicating whether the handler has been disposed.</summary>
    public bool IsDisposed { get; private set; }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        base.Dispose(disposing);
    }
}
