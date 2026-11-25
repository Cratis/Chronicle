// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_validating_extended_mappings : given.a_runtime_language_service
{
    [Theory]
    [InlineData("set once missing = name")]
    [InlineData("set once age = name")]
    [InlineData("set once name = missing")]
    [InlineData("remove entries.$eventContent.missing")]
    [InlineData("remove age.$eventContent.entry")]
    [InlineData("add total by left * missing")]
    [InlineData("subtract total by left * name")]
    public void should_reject_invalid_schema_paths_and_types(string mapping)
    {
        var result = _language.Compile($"projection Alerts => Alert\n  from Changed\n    {mapping}\n", ProjectionOwner.Client, [_readModel], _schemas);
        result.Match(_ => false, errors => errors.HasErrors).ShouldBeTrue();
    }

    [Theory]
    [InlineData("remove name")]
    [InlineData("remove entries.$unknown")]
    [InlineData("remove entries.$eventContent")]
    [InlineData("remove entries.$eventContext.missing")]
    [InlineData("remove entries.$causedBy.missing")]
    public void should_reject_invalid_dictionary_targets_even_without_schemas(string mapping)
    {
        var result = _language.Compile($"projection Alerts => Alert\n  from Changed\n    {mapping}\n", ProjectionOwner.Client, [], []);
        result.Match(_ => false, errors => errors.HasErrors).ShouldBeTrue();
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("details")]
    public void should_reject_invalid_sort_properties(string property)
    {
        var declaration = $"""
            projection Alerts => Alert
              children notes identified by id
                sort by {property} ascending
                from Changed key id
                  parent $eventSourceId
                  name = name
            """;
        var result = _language.Compile(declaration, ProjectionOwner.Client, [_readModel], _schemas);
        result.Match(_ => false, errors => errors.HasErrors).ShouldBeTrue();
    }
}
