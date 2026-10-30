// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_finding_in_primary;

/// <summary>
/// MongoDB Atlas shared and Flex tiers reject any listCollections name filter that is not a plain string
/// or a regular expression, so an operator document such as $in fails every lookup by key (#5431).
/// </summary>
public class and_the_key_is_absent_so_the_collection_state_is_listed : given.a_sink_observing_a_container
{
    void Establish() => _primaryId = Guid.NewGuid();

    async Task Because() => await _sink.FindInPrimaryOrDefault(new Key("missing", ArrayIndexers.NoIndexers));

    [Fact] void should_list_collections() => _listCollectionsFilters.ShouldNotBeEmpty();
    [Fact] void should_filter_name_with_a_regular_expression_only() => _listCollectionsFilters.ShouldEachConformTo(filter => filter["name"].IsBsonRegularExpression);
    [Fact] void should_match_both_the_primary_and_the_promoting_collection_exactly() =>
        _listCollectionsFilters.ShouldEachConformTo(filter =>
            filter["name"].AsBsonRegularExpression.Pattern == $"^({Regex.Escape(_readModel.ContainerName.Value)}|{Regex.Escape(PromotingName)})$");
}
