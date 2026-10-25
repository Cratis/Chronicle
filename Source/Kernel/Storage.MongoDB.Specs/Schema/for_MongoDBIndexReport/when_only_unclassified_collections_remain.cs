// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBIndexReport;

public class when_only_unclassified_collections_remain : Specification
{
    MongoDBIndexReport _report;
    bool _current;

    void Establish() => _report = new(
    [
        new("namespace:orders/Default", "orders+es+Default", [], [])
        {
            Unclassified = ["event-log+unknown+constraint"]
        }
    ]);

    void Because() => _current = _report.IsCurrent;

    [Fact] void should_not_make_the_report_non_current() => _current.ShouldBeTrue();
    [Fact] void should_preserve_the_unclassified_collection_names() => _report.Scopes[0].Unclassified[0].ShouldEqual("event-log+unknown+constraint");
}
