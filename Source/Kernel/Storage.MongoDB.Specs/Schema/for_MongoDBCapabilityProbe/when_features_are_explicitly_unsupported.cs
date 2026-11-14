// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBCapabilityProbe;

public class when_features_are_explicitly_unsupported : Specification
{
    Exception[] _errors;
    MongoDBCapabilityReport[] _reports;

    void Establish() => _errors =
    [
        given.command_errors.Create(115, "CommandNotSupported"),
        given.command_errors.Create(238, "NotImplemented"),
        given.command_errors.Create(40324, "Stage $changeStream is not supported yet in native pipeline"),
        given.command_errors.Create(2, "createIndex.collation is not implemented yet"),
        given.command_errors.Create(40324, "Unrecognized pipeline stage name: '$changeStream'"),
        given.command_errors.Create(2, "COLLATION IS NOT IMPLEMENTED YET")
    ];

    async Task Because() => _reports = await Task.WhenAll(_errors.Select(error =>
        new MongoDBCapabilityProbe(_ => Task.FromException(error), _ => Task.FromException(error)).Probe(CancellationToken.None)));

    [Fact] void should_reject_change_streams() => _reports.All(report => report.ChangeStreams.State == MongoDBCapabilityState.Unsupported).ShouldBeTrue();
    [Fact] void should_reject_collation() => _reports.All(report => report.Collation.State == MongoDBCapabilityState.Unsupported).ShouldBeTrue();
    [Fact] void should_list_both_missing_features() => _reports.All(report => report.Failures.SequenceEqual(["change streams", "collation"])).ShouldBeTrue();
}
