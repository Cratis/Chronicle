// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBCapabilityProbe;

public class when_commands_are_inconclusive : Specification
{
    Exception[] _errors;
    MongoDBCapabilityReport[] _reports;

    void Establish() => _errors =
    [
        given.command_errors.Create(13, "Unauthorized: not implemented for this user"),
        given.command_errors.Create(18, "AuthenticationFailed"),
        new TimeoutException("mongodb://user:secret@host"),
        new MongoConnectionException(new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017))), "unreachable"),
        new IOException("connection closed"),
        new OperationCanceledException()
    ];

    async Task Because() => _reports = await Task.WhenAll(_errors.Select(error =>
        new MongoDBCapabilityProbe(_ => Task.FromException(error), _ => Task.CompletedTask).Probe(CancellationToken.None)));

    [Fact] void should_not_reject_the_service() => _reports.All(report => !report.Failures.Any()).ShouldBeTrue();
    [Fact] void should_report_change_streams_as_inconclusive() => _reports.All(report => report.ChangeStreams.State == MongoDBCapabilityState.Inconclusive).ShouldBeTrue();
    [Fact] void should_still_probe_collation() => _reports.All(report => report.Collation == MongoDBCapability.Supported).ShouldBeTrue();
    [Fact] void should_not_expose_credentials() => _reports.All(report => !report.ChangeStreams.Reason.Contains("secret", StringComparison.Ordinal)).ShouldBeTrue();
}
