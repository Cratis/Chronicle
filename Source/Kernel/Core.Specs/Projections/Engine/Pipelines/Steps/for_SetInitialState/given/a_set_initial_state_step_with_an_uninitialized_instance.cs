// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SetInitialState.given;

public class a_set_initial_state_step_with_an_uninitialized_instance : a_set_initial_state_step
{
    void Establish()
    {
        var stored = new ExpandoObject();
        var storedAsDictionary = (IDictionary<string, object?>)stored;
        storedAsDictionary["id"] = "the-key";
        storedAsDictionary["items"] = new List<object>();
        storedAsDictionary[WellKnownProperties.ReadModelInstanceInitialized] = false;
        _sink.FindOrDefault(Arg.Any<Key>()).Returns(stored);

        var initialModelState = new ExpandoObject();
        var initialModelStateAsDictionary = (IDictionary<string, object?>)initialModelState;
        initialModelStateAsDictionary["id"] = string.Empty;
        initialModelStateAsDictionary["status"] = "open";
        initialModelStateAsDictionary["items"] = new List<object>();
        _projection.InitialModelState.Returns(initialModelState);
    }

    protected static IEnumerable<PropertyPath> ChangedProperties(ProjectionEventContext context) =>
        context.Changeset.Changes
            .OfType<PropertiesChanged<ExpandoObject>>()
            .SelectMany(_ => _.Differences)
            .Select(_ => _.PropertyPath);
}
