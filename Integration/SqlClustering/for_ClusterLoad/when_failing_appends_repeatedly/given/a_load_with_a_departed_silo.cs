// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoad.when_failing_appends_repeatedly.given;

public class a_load_with_a_departed_silo : Specification
{
    protected ClusterLoad _load;
    protected Exception _appendFailure;
    protected Exception _error;
    protected int _attempts;
    protected readonly MembershipEntry _departed = new("172.20.0.5", 151569171, 3);
    protected readonly a_clock _clock = new();

    void Establish()
    {
        _load = new(timeProvider: _clock);
        _load.SiloDeparting(_departed);
        _appendFailure = ConnectionFailure(_departed.Generation);
    }

    protected static Exception ConnectionFailure(int generation)
    {
        // Contracts is a private client dependency; construct its real exception without adding a public reference.
        var contracts = Assembly.Load("Cratis.Chronicle.Contracts");
        var resultType = contracts.GetType("Cratis.Chronicle.Contracts.Commands.CommandResult", throwOnError: true)!;
        var result = Activator.CreateInstance(resultType)!;
        resultType.GetProperty("ExceptionMessages")!.SetValue(result, new List<string>
        {
            $"Exception while sending message: Orleans.Runtime.Messaging.ConnectionFailedException: Unable to connect to endpoint S172.20.0.5:11111:{generation}. See InnerException\n ---> Orleans.Networking.Shared.SocketConnectionException: Unable to connect to 172.20.0.5:11111. Error: ConnectionRefused"
        });
        var exceptionType = contracts.GetType("Cratis.Chronicle.Contracts.Commands.CommandFailed", throwOnError: true)!;
        return (Exception)Activator.CreateInstance(exceptionType, result)!;
    }

    protected void FailAppends(int count)
    {
        for (var attempt = 0; attempt < count; attempt++)
        {
            _attempts++;
            _load.RecordFailedAppend(Guid.NewGuid(), _appendFailure);
        }
    }

    async Task Destroy() => await _load.DisposeAsync();

    protected class a_clock : TimeProvider
    {
        long _timestamp = TimeSpan.TicksPerDay;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _timestamp;

        public void Advance(TimeSpan elapsed) => _timestamp += elapsed.Ticks;
    }
}
