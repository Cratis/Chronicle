// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBIndexReport;

public class when_only_incompatible_indexes_remain : Specification
{
    MongoDBIndexReport _report;
    bool _current;

    void Establish() => _report = new(
    [
        new("namespace:orders/Default", "orders+es+Default", [], [])
        {
            Incompatible = ["event-log/publication_identity"]
        }
    ]);

    void Because() => _current = _report.IsCurrent;

    [Fact] void should_make_the_report_non_current() => _current.ShouldBeFalse();
    [Fact] void should_preserve_the_incompatible_index_names() => _report.Scopes[0].Incompatible[0].ShouldEqual("event-log/publication_identity");
}
