// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Aksio.Cratis.Applications.Queries
{
    /// <summary>
    /// Represents an implementation of <see cref="IClientObservable"/>.
    /// </summary>
    /// <typeparam name="T">Type of data being observed.</typeparam>
    public class ClientObservable<T> : IClientObservable, IAsyncEnumerable<T>
    {
        readonly ReplaySubject<T> _subject = new();

        /// <summary>
        /// Gets or sets the callback that gets called when the client disconnects.
        /// </summary>
        public Action? ClientDisconnected { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientObservable{T}"/> class.
        /// </summary>
        /// <param name="clientDisconnected">Optional callback that gets called when client is disconnected.</param>
        public ClientObservable(Action? clientDisconnected = default)
        {
            ClientDisconnected = clientDisconnected;
        }

        /// <summary>
        /// Notifies all subscribed and future observers about the arrival of the specified element in the sequence.
        /// </summary>
        /// <param name="next">The value to send to all observers.</param>
        public void OnNext(T next) => _subject.OnNext(next);

        /// <inheritdoc/>
        public async Task HandleConnection(ActionExecutingContext context, JsonOptions jsonOptions)
        {
            using var webSocket = await context.HttpContext.WebSockets.AcceptWebSocketAsync();
            var subscription = _subject.Subscribe(_ =>
            {
                var queryResult = new QueryResult(_!, true);
                var json = JsonSerializer.Serialize(queryResult, jsonOptions.JsonSerializerOptions);
                var message = Encoding.UTF8.GetBytes(json);

                webSocket.SendAsync(new ArraySegment<byte>(message, 0, message.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            });

            var buffer = new byte[1024 * 4];
            try
            {
                var received = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                while (!received.CloseStatus.HasValue)
                {
                    received = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }

                await webSocket.CloseAsync(received.CloseStatus.Value, received.CloseStatusDescription, CancellationToken.None);
            }
            finally
            {
                subscription.Dispose();
                ClientDisconnected?.Invoke();
            }
        }

        /// <inheritdoc/>
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new ObservableAsyncEnumerator<T>(_subject, cancellationToken);

        /// <inheritdoc/>
        public object GetAsynchronousEnumerator(CancellationToken cancellationToken = default) => GetAsyncEnumerator(cancellationToken);
    }
}
