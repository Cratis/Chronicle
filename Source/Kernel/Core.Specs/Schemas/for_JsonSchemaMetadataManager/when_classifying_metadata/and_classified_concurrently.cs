// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_classifying_metadata;

public class and_classified_concurrently : given.real_pii_protection
{
    ConcurrentBag<string> _classifications;

    void Establish()
    {
        _schema = new JsonSchema { Type = JsonObjectType.String };
        Mark(_schema);
        _classifications = [];
    }

    void Because() => Parallel.For(0, 32, _ =>
        _classifications.Add(string.Join(",", _manager.MetadataFor(_schema, forComparison: false).Select(entry => $"{entry.Category.Value}:{entry.Metadata.metadataType}"))));

    [Fact] void should_classify_every_call() => _classifications.Count.ShouldEqual(32);
    [Fact] void should_return_equal_classifications() => _classifications.Distinct().ShouldContainOnly("Compliance:PII");
}
