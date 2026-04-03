// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_Generator.when_generating;

public class with_read_model_that_has_no_schema_title : given.a_generator
{
    const string ReadModelIdentifier = "MyReadModel";
    const string ProjectionName = "MyProjection";

    void Establish()
    {
        var readModelDefinition = CreateReadModelDefinition(ReadModelIdentifier, schemaTitle: null);
        var projectionDefinition = CreateProjectionDefinition(ProjectionName, readModelDefinition.Identifier);

        _result = _generator.Generate(projectionDefinition, readModelDefinition);
    }

    [Fact] void should_generate_declaration() => _result.ShouldNotBeNull();
    [Fact] void should_use_identifier_as_read_model_name() => _result.ShouldContain($"projection {ProjectionName} => {ReadModelIdentifier}");
}
