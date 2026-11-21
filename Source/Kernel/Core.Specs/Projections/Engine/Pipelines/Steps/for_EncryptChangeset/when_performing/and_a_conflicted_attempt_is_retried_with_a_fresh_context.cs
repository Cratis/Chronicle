// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_EncryptChangeset.when_performing;

public class and_a_conflicted_attempt_is_retried_with_a_fresh_context : given.all_dependencies
{
    readonly List<string?> _valuesHandedToCompliance = [];
    ProjectionEventContext _firstAttempt;
    ProjectionEventContext _secondAttempt;

    void Establish()
    {
        _schema.Properties["name"] = new JsonSchemaProperty
        {
            ExtensionData = new Dictionary<string, object?>
            {
                { ComplianceJsonSchemaExtensions.ComplianceKey, new[] { new ComplianceSchemaMetadata("PII", string.Empty) } }
            }
        };

        // The state of an attempt is whatever the read path decrypted: always the plaintext, never what an earlier attempt produced.
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(_ => new JsonObject { ["name"] = "plaintext" });
        _complianceManager.ApplyToReadModel(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(call =>
            {
                var input = call.ArgAt<JsonObject>(4);
                _valuesHandedToCompliance.Add(input["name"]?.GetValue<string>());
                return Task.FromResult(new JsonObject { ["name"] = $"enc({input["name"]!.GetValue<string>()})" });
            });

        _firstAttempt = CreateContext(EventSourceIdValue);
        _secondAttempt = CreateContext(EventSourceIdValue);
    }

    async Task Because()
    {
        await _step.Perform(_projection, _firstAttempt);
        await _step.Perform(_projection, _secondAttempt);
    }

    [Fact] void should_apply_protection_once_per_attempt() => _valuesHandedToCompliance.Count.ShouldEqual(2);
    [Fact] void should_hand_the_plaintext_to_protection_in_every_attempt() => _valuesHandedToCompliance.ShouldEachConformTo(_ => _ == "plaintext");
    [Fact] void should_not_share_state_between_the_attempts() => ReferenceEquals(_secondAttempt.Changeset.CurrentState, _firstAttempt.Changeset.CurrentState).ShouldBeFalse();
}
