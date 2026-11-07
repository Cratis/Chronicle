// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Storage.Vault.for_EncryptionKeyStorageContract.given;

/// <summary>
/// Pause real SDK requests at the validation/publication boundary, never using clock delays.
/// </summary>
/// <param name="inner">The fake or real server transport.</param>
public sealed class VaultInterleaving(HttpMessageHandler inner) : DelegatingHandler(inner)
{
    Func<HttpRequestMessage, JsonElement?, bool>? _pause;
    Func<HttpRequestMessage, JsonElement?, bool>? _lose;
    TaskCompletionSource _paused = new(TaskCreationOptions.RunContinuationsAsynchronously);
    TaskCompletionSource _resume = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool SawForwardingHeader { get; private set; }

    public Task DuringPublication(Func<Task> operation, Func<Task> competingOperation) => During(
        (request, data) => IsDataWrite(request) && data?.TryGetProperty("published", out var published) == true && published.GetBoolean(), operation, competingOperation);

    public Task DuringErasure(Func<Task> operation, Func<Task> competingOperation) => During(
        (request, data) => IsDataWrite(request) && request.RequestUri!.AbsolutePath.EndsWith("/erasure", StringComparison.Ordinal) && data?.TryGetProperty("requests", out var requests) == true && requests.GetString() != "[]", operation, competingOperation);

    public Task DuringTombstone(Func<Task> operation, Func<Task> competingOperation) => During(
        (request, data) => IsDataWrite(request) && data?.TryGetProperty("erasedTombstone", out _) == true, operation, competingOperation);

    public async Task LoseWriteResponse(Func<Task> operation)
    {
        _lose = (request, data) => IsDataWrite(request) && request.RequestUri!.AbsolutePath.EndsWith("/erasure", StringComparison.Ordinal) && data?.TryGetProperty("requests", out var requests) == true && requests.GetString() != "[]";
        try
        {
            await operation();
        }
        finally
        {
            _lose = null;
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var body = request.Content is null ? null : JsonDocument.Parse(await request.Content.ReadAsStringAsync(cancellationToken));
        JsonElement? data = body?.RootElement.TryGetProperty("data", out var value) == true ? value : null;
        SawForwardingHeader |= request.Headers.TryGetValues("X-Vault-Inconsistent", out var headers) && headers.Contains("forward-active-node", StringComparer.Ordinal);
        if (_pause?.Invoke(request, data) == true)
        {
            _pause = null;
            var resume = _resume.Task;
            _paused.TrySetResult();
            await resume;
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (_lose?.Invoke(request, data) == true)
        {
            _lose = null;
            response.Dispose();
            throw new TimeoutException("The successful Vault write response was lost.");
        }

        return response;
    }

    static bool IsDataWrite(HttpRequestMessage request) => (request.Method == HttpMethod.Put || request.Method == HttpMethod.Post) && request.RequestUri!.AbsolutePath.Contains("/data/", StringComparison.Ordinal);

    async Task During(Func<HttpRequestMessage, JsonElement?, bool> predicate, Func<Task> operation, Func<Task> competingOperation)
    {
        var paused = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _paused = paused;
        _resume = resume;
        _pause = predicate;
        var pending = operation();
        try
        {
            var completed = await Task.WhenAny(paused.Task, pending);
            if (completed == pending) await pending;
            await paused.Task.WaitAsync(TimeSpan.FromSeconds(30));
            await competingOperation();
        }
        finally
        {
            _pause = null;
            resume.TrySetResult();
        }

        await pending;
    }
}
