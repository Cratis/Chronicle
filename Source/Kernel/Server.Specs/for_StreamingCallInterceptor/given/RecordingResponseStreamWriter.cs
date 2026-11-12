// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Server.for_StreamingCallInterceptor.given;

public sealed class RecordingResponseStreamWriter : IServerStreamWriter<string>
{
    public List<string> Items { get; } = [];
    public WriteOptions? WriteOptions { get; set; }
    public TaskCompletionSource WriteStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task Completion { get; set; } = Task.CompletedTask;

    public async Task WriteAsync(string message)
    {
        WriteStarted.TrySetResult();
        await Completion;
        Items.Add(message);
    }
}
