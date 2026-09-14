// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Cratis.Chronicle.Concepts.Projections.Definitions.for_ProjectionDefinition;

/// <summary>
/// A projection grain's state is left uninitialized - every reference-typed member null, including <see cref="ProjectionDefinition.From"/>
/// and <see cref="ProjectionDefinition.Children"/> despite their non-nullable type - when no definition has been persisted for it yet.
/// Orleans deep-copies grain-call responses through System.Text.Json, which used to NRE evaluating <see cref="ProjectionDefinition.IsEmpty"/>
/// against exactly this state (#3934).
/// </summary>
public class when_the_definition_is_uninitialized : Specification
{
    static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    ProjectionDefinition _definition;
    Exception _serializationError;
    bool _isEmpty;

    void Establish() => _definition = (ProjectionDefinition)RuntimeHelpers.GetUninitializedObject(typeof(ProjectionDefinition));

    async Task Because()
    {
        _serializationError = await Catch.Exception(() => Task.Run(() => JsonSerializer.Serialize(_definition, _options)));
        _isEmpty = _definition.IsEmpty;
    }

    [Fact] void should_serialize_without_throwing() => _serializationError.ShouldBeNull();
    [Fact] void should_report_itself_as_empty() => _isEmpty.ShouldBeTrue();
}
