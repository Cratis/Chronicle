// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Registrations;
using Cratis.Serialization;

namespace Cratis.Chronicle.for_ArtifactIdentity;

public class when_deriving_model_projection_ids : Specification
{
    readonly List<string> _discovered = [];
    readonly List<string> _explicitModelBound = [];
    string[] _callbackDefaults;
    string _emptyCallbackId;
    string _suppliedCallbackId;

    void Because()
    {
        var builder = new ModelBoundProjectionBuilder(new DefaultNamingPolicy(), RegisteredEventTypesForSpecifications.Create());
        foreach (var type in new[] { typeof(Model), typeof(GenericModel<int>) })
        {
            _discovered.Add(builder.Build(type).Identifier);
            _explicitModelBound.Add(new ModelBoundExplicitProjection(type).Id.Value);
        }

        var defaults = new ExplicitArtifacts().RegisterProjection<Model>(_ => { }).RegisterProjection<GenericModel<int>>(_ => { });
        _callbackDefaults = defaults.Projections.Select(projection => projection.Id.Value).ToArray();
        _emptyCallbackId = new ExplicitArtifacts().RegisterProjection<Model>(_ => { }, "").Projections.Single().Id.Value;
        _suppliedCallbackId = new ExplicitArtifacts().RegisterProjection<Model>(_ => { }, "supplied").Projections.Single().Id.Value;
    }

    [Fact] void should_keep_discovered_full_names() => _discovered.ShouldEqual([typeof(Model).FullName!, typeof(GenericModel<int>).FullName!]);
    [Fact] void should_keep_explicit_model_bound_full_names() => _explicitModelBound.ShouldEqual([typeof(Model).FullName!, typeof(GenericModel<int>).FullName!]);
    [Fact] void should_keep_callback_default_full_names() => _callbackDefaults.ShouldEqual([typeof(Model).FullName!, typeof(GenericModel<int>).FullName!]);
    [Fact] void should_preserve_an_empty_supplied_id() => _emptyCallbackId.ShouldEqual("");
    [Fact] void should_preserve_a_supplied_id() => _suppliedCallbackId.ShouldEqual("supplied");

    public record Model(string Name);
    public record GenericModel<T>(string Name);
}
