// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cratis.Queries;

/// <summary>
/// Represents an implementation of <see cref="IClientObservable"/>.
/// </summary>
/// <typeparam name="T">Type of data being observed.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ClientObservable{T}"/> class.
/// </remarks>
/// <param name="clientDisconnected">Optional callback that gets called when client is disconnected.</param>
public class ClientObservable<T>(Action? clientDisconnected = default) : IClientObservable, IAsyncEnumerable<T>, IDisposable
{
    readonly ReplaySubject<T> _subject = new();

    /// <inheritdoc/>
    public bool IsDisposed => _subject.IsDisposed;

    /// <summary>
    /// Gets or sets the callback that gets called when the client disconnects.
    /// </summary>
    public Action? ClientDisconnected { get; set; } = clientDisconnected;

    /// <inheritdoc/>
    public void Dispose() => _subject.Dispose();

    /// <summary>
    /// Notifies all subscribed and future observers about the arrival of the specified element in the sequence.
    /// </summary>
    /// <param name="next">The value to send to all observers.</param>
    public void OnNext(T next) => _subject.OnNext(next);

    /// <inheritdoc/>
    public async Task HandleConnection(ActionExecutingContext context, JsonOptions jsonOptions)
    {
        using var webSocket = await context.HttpContext.WebSockets.AcceptWebSocketAsync();
        IDisposable? subscription = default;
        var queryResult = new QueryResult();
        using var cts = new CancellationTokenSource();

#pragma warning disable MA0147 // Avoid async void method for delegate
        subscription = _subject.Subscribe(async _ =>
        {
            queryResult.Data = _!;

            try
            {
                var message = JsonSerializer.SerializeToUtf8Bytes(queryResult, jsonOptions.JsonSerializerOptions);
                await webSocket.SendAsync(message, WebSocketMessageType.Text, true, cts.Token);
                message = null!;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error sending message to client: {ex.Message}\n{ex.StackTrace}");
                subscription?.Dispose();
                ClientDisconnected?.Invoke();
            }
        });
#pragma warning restore MA0147 // Avoid async void method for delegate

        var buffer = new byte[1024 * 4];
        try
        {
            var received = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);

            while (!received.CloseStatus.HasValue)
            {
                received = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
            }

            await webSocket.CloseAsync(received.CloseStatus.Value, received.CloseStatusDescription, cts.Token);
        }
        catch
        {
            Console.WriteLine("Client disconnected");
        }
        finally
        {
            cts.Cancel();
            _subject.OnCompleted();
            subscription?.Dispose();
            ClientDisconnected?.Invoke();
        }
    }

    /// <inheritdoc/>
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new ObservableAsyncEnumerator<T>(_subject, cancellationToken);

    /// <inheritdoc/>
    public object GetAsynchronousEnumerator(CancellationToken cancellationToken = default) => GetAsyncEnumerator(cancellationToken);
}
