// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_creating_the_join_filter_target.given;

/// <summary>
/// A <see cref="ChangesetConverter"/> over a real <see cref="MongoDBConverter"/> and a real schema, so the
/// join filter is built through the actual schema lookup and type conversion rather than a substitute.
/// </summary>
/// <remarks>
/// The join key a root-level join arrives with is the join source's raw event source id — always a string —
/// while the joined-on column is stored in whatever representation its schema dictates. Every combination of
/// the two is specified here, and the two invariants asserted throughout are that building the filter never
/// throws and that the comparand is the one the column can actually be matched against.
/// </remarks>
public class a_changeset_converter_over_typed_columns : Specification
{
    protected const string RootKeyValue = "root-1";

    protected ChangesetConverter _converter;

    void Establish()
    {
        var schema = JsonSchema.FromType<JoinTargetReadModel>();
        var readModel = new ReadModelDefinition(
            typeof(JoinTargetReadModel).FullName,
            nameof(JoinTargetReadModel),
            nameof(JoinTargetReadModel),
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, schema }
            },
            []);

        var typeFormats = new TypeFormats();
        var expandoObjectConverter = new ExpandoObjectConverter(typeFormats);
        _converter = new ChangesetConverter(
            readModel,
            new MongoDBConverter(expandoObjectConverter, typeFormats, readModel, NullLogger<MongoDBConverter>.Instance),
            Substitute.For<ISinkCollections>(),
            expandoObjectConverter);
    }

    protected static Key RootKey() => new(RootKeyValue, ArrayIndexers.NoIndexers);

    protected static Key ChildKey(PropertyPath childrenProperty, PropertyPath identifiedByProperty, object? identifier) =>
        new(RootKeyValue, new ArrayIndexers([new ArrayIndexer(childrenProperty, identifiedByProperty, identifier!)]));

    protected static Joined JoinOn(PropertyPath onProperty, object? key) =>
        new(new ExpandoObject(), key!, onProperty, ArrayIndexers.NoIndexers, []);
}
