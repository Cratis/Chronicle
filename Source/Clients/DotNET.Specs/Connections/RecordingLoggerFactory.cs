// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

public class RecordingLoggerFactory : ILoggerFactory
{
    public List<RecordedLog> Entries { get; } = [];

    public ILogger CreateLogger(string categoryName) => new CapturingLogger<object>(Entries);

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public void Dispose()
    {
    }
}
