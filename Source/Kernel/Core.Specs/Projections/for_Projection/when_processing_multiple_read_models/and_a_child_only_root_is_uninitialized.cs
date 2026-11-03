// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_multiple_read_models;

public class and_a_child_only_root_is_uninitialized : Specification
{
    Engine.IProjection _projection;
    ExpandoObject _state;
    bool _visible;

    void Establish()
    {
        _projection = Substitute.For<Engine.IProjection>();
        _projection.HasParent.Returns(false);
        _projection.SubscribesToAllEvents.Returns(false);
        _projection.ChildProjections.Returns([Substitute.For<Engine.IProjection>()]);
        _projection.OperationTypes.Returns(new Dictionary<EventType, ProjectionOperationType>());
        _state = new ExpandoObject();
        ((IDictionary<string, object?>)_state)[WellKnownProperties.ReadModelInstanceInitialized] = false;
    }

    void Because() => _visible = ProjectionInitialization.IsChildOnly(_projection) || !ReadModelInitialization.IsUninitialized(_state);

    [Fact] void should_return_the_root() => _visible.ShouldBeTrue();
    [Fact] void should_keep_the_root_uninitialized() => ((IDictionary<string, object?>)_state)[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(false);
}
