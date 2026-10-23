// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_a_value_is_missing : given.an_index
{
    void Establish()
    {
        Set("root", Children("present"));
    }

    void Because()
    {
        _matches = Find("[children].id", "missing");
    }

    [Fact] void should_return_no_roots() => _matches.ShouldBeEmpty();
}
