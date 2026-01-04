// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.Logging;

namespace Chronicle.Benchmarks;

public class ChronicleClientHelper : IDisposable
{
    readonly ChronicleClient _client;
    readonly IEventStore _eventStore;
    readonly ILoggerFactory _loggerFactory;

    public ChronicleClientHelper(string url = "http://localhost:35000")
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        var options = new ChronicleOptions(
            url: new ChronicleUrl(url),
            connectTimeout: 30,
            loggerFactory: _loggerFactory);

        _client = new ChronicleClient(options);
        _eventStore = _client.GetEventStore("benchmarks").GetAwaiter().GetResult();
    }

    public IEventLog EventLog => _eventStore.EventLog;

    public async Task WaitForConnection()
    {
        // Attempt a simple operation to verify connection is ready
        var retries = 5;
        var delay = 200;

        for (var i = 0; i < retries; i++)
        {
            try
            {
                await _eventStore.EventLog.GetTailSequenceNumber();
                return;
            }
            catch
            {
                if (i == retries - 1) throw;
                await Task.Delay(delay);
                delay *= 2;
            }
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
        _loggerFactory?.Dispose();
        GC.SuppressFinalize(this);
    }
}
