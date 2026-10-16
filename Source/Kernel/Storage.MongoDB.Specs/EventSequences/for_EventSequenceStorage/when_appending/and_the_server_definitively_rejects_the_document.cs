// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reflection;
using Cratis.Chronicle.Storage.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending;

public class and_the_server_definitively_rejects_the_document : Specification
{
    readonly Dictionary<int, Exception> _classified = [];

    void Because()
    {
        foreach (var code in new[] { 121, 10334, 17419, 17420 })
        {
            _classified[code] = EventSequenceStorage.ClassifySingleAppendFailure(Rejected(code), true);
        }
    }

    [Fact] void should_report_document_validation_as_not_committed() => _classified[121].ShouldBeOfExactType<EventsNotCommitted>();
    [Fact] void should_report_bson_object_too_large_as_not_committed() => _classified[10334].ShouldBeOfExactType<EventsNotCommitted>();
    [Fact] void should_report_the_legacy_update_size_rejection_as_not_committed() => _classified[17419].ShouldBeOfExactType<EventsNotCommitted>();
    [Fact] void should_report_the_legacy_upsert_size_rejection_as_not_committed() => _classified[17420].ShouldBeOfExactType<EventsNotCommitted>();
    [Fact] void should_keep_unlisted_write_errors_unknown() => Classify(Rejected(64)).ShouldBeOfExactType<MongoWriteException>();
    [Fact] void should_not_classify_other_categories_as_definite_rejections() => Classify(Rejected(121, ServerErrorCategory.DuplicateKey)).ShouldBeOfExactType<MongoWriteException>();
    [Fact] void should_not_hide_a_write_concern_error() => Classify(Rejected(121, concern: true)).ShouldBeOfExactType<MongoWriteException>();
    [Fact] void should_not_hide_an_unknown_commit_label() => Classify(Rejected(121, unknownCommit: true)).ShouldBeOfExactType<MongoWriteException>();
    [Fact] void should_not_change_batch_classification() => EventSequenceStorage.ClassifyAppendFailure(Rejected(121), true).ShouldBeOfExactType<MongoWriteException>();

    static Exception Classify(MongoWriteException error) => EventSequenceStorage.ClassifySingleAppendFailure(error, true);

    static MongoWriteException Rejected(int code, ServerErrorCategory category = ServerErrorCategory.Uncategorized, bool concern = false, bool unknownCommit = false)
    {
        // These driver types expose their values publicly but keep their constructors internal.
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var write = (WriteError)Activator.CreateInstance(typeof(WriteError), flags, null, [category, code, "Document rejected", new BsonDocument()], null)!;
        var writeConcern = concern ? (WriteConcernError)Activator.CreateInstance(typeof(WriteConcernError), flags, null, [64, "WriteConcernFailed", "Uncertain concern", new BsonDocument(), Array.Empty<string>()], null)! : null;
        var connection = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));
        var error = new MongoWriteException(connection, write, writeConcern, null);
        if (unknownCommit) error.AddErrorLabel("UnknownTransactionCommitResult");
        return error;
    }
}
