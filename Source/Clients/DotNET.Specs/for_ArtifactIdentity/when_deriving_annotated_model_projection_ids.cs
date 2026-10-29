// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Registrations;
using Cratis.Serialization;

namespace Cratis.Chronicle.for_ArtifactIdentity;

public class when_deriving_annotated_model_projection_ids : Specification
{
    readonly Dictionary<Type, string> _ids = [];
    readonly Dictionary<Type, string> _definitions = [];
    readonly Dictionary<Type, string> _explicitIds = [];
    string _callbackDefault;
    string _callbackOverride;
    string _emptyCallbackOverride;
    EventSequenceId _eventSequence;

    void Because()
    {
        var builder = new ModelBoundProjectionBuilder(new DefaultNamingPolicy(), RegisteredEventTypesForSpecifications.Create());
        foreach (var type in new[] { typeof(Model), typeof(ClassModel), typeof(StructModel), typeof(EmptyModel), typeof(DefaultModel), typeof(WhitespaceModel), typeof(GenericModel<int>) })
        {
            _ids[type] = ArtifactIdentity.ModelProjectionIdFor(type).Value;
            _definitions[type] = builder.Build(type).Identifier;
            _explicitIds[type] = new ModelBoundExplicitProjection(type).Id.Value;
        }

        _callbackDefault = new ExplicitArtifacts().RegisterProjection<Model>(_ => { }).Projections.Single().Id.Value;
        _callbackOverride = new ExplicitArtifacts().RegisterProjection<Model>(_ => { }, "callback").Projections.Single().Id.Value;
        _emptyCallbackOverride = new ExplicitArtifacts().RegisterProjection<Model>(_ => { }, "").Projections.Single().Id.Value;
        _eventSequence = builder.Build(typeof(Model)).EventSequenceId;
    }

    [Fact] void should_use_the_declared_record_id() => _ids[typeof(Model)].ShouldEqual("old-id");
    [Fact] void should_use_the_declared_class_id() => _ids[typeof(ClassModel)].ShouldEqual("class-id");
    [Fact] void should_use_the_declared_record_struct_id() => _ids[typeof(StructModel)].ShouldEqual("struct-id");
    [Fact] void should_fall_back_for_an_empty_id() => _ids[typeof(EmptyModel)].ShouldEqual(typeof(EmptyModel).FullName);
    [Fact] void should_fall_back_for_a_default_attribute() => _ids[typeof(DefaultModel)].ShouldEqual(typeof(DefaultModel).FullName);
    [Fact] void should_not_trim_an_id() => _ids[typeof(WhitespaceModel)].ShouldEqual(" ");
    [Fact] void should_keep_generic_full_names() => _ids[typeof(GenericModel<int>)].ShouldEqual(typeof(GenericModel<int>).FullName);
    [Fact] void should_agree_with_builder_definitions() => _definitions.All(pair => pair.Value == _ids[pair.Key]).ShouldBeTrue();
    [Fact] void should_agree_with_explicit_model_bound_recipes() => _explicitIds.All(pair => pair.Value == _ids[pair.Key]).ShouldBeTrue();
    [Fact] void should_use_the_attribute_for_callback_defaults() => _callbackDefault.ShouldEqual("old-id");
    [Fact] void should_prefer_a_supplied_callback_id() => _callbackOverride.ShouldEqual("callback");
    [Fact] void should_preserve_an_explicitly_empty_callback_id() => _emptyCallbackOverride.ShouldEqual("");
    [Fact] void should_not_use_projection_event_sequence_metadata_for_model_bound_routing() => _eventSequence.ShouldEqual(EventSequenceId.Log);
    [Fact] void should_not_change_read_model_identity() => ArtifactIdentity.ReadModelIdFor(typeof(Model)).ShouldEqual(typeof(Model).FullName);

    [Projection("old-id", "ignored")]
    public record Model(string Name);

    [Projection("class-id")]
    public class ClassModel;

    [Projection("struct-id")]
    public readonly record struct StructModel(string Name);

    [Projection("")]
    public record EmptyModel;

    [Projection]
    public record DefaultModel;

    [Projection(" ")]
    public record WhitespaceModel;

    public record GenericModel<T>;
}
