// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_ProjectionRegistrationSignature;

public class a_registration : Specification
{
    protected static readonly EventType FirstEvent = new("e1111111-1111-1111-1111-111111111111", 1);
    protected static readonly EventType SecondEvent = new("e2222222-2222-2222-2222-222222222222", 1);

    protected static ProjectionDefinition Definition(string identifier = "the-projection") => new(
        ProjectionOwner.Client,
        EventSequenceId.Log,
        identifier,
        "the-read-model",
        true,
        true,
        new JsonObject(),
        new Dictionary<EventType, FromDefinition>
        {
            [FirstEvent] = new(new Dictionary<PropertyPath, string> { [(PropertyPath)"name"] = "name" }, PropertyExpression.NotSet, null),
            [SecondEvent] = new(new Dictionary<PropertyPath, string> { [(PropertyPath)"other"] = "other" }, PropertyExpression.NotSet, null)
        },
        new Dictionary<EventType, JoinDefinition>(),
        new Dictionary<PropertyPath, ChildrenDefinition>(),
        [],
        new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>());

    protected static string SignatureOf(params ProjectionDefinition[] definitions) =>
        ProjectionRegistrationSignature.For(definitions, null, "the-app", "1.0.0");
}
