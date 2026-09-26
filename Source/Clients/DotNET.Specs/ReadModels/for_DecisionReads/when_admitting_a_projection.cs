// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_DecisionReads;

public class when_admitting_a_projection : given.a_decision_reader
{
    [Theory]
    [InlineData(DecisionReadRefusalReason.Reducer)]
    [InlineData(DecisionReadRefusalReason.AmbiguousProjection)]
    [InlineData(DecisionReadRefusalReason.NotEventLog)]
    [InlineData(DecisionReadRefusalReason.Join)]
    [InlineData(DecisionReadRefusalReason.Hierarchy)]
    [InlineData(DecisionReadRefusalReason.OpenEndedEventTypes)]
    [InlineData(DecisionReadRefusalReason.Derivatives)]
    [InlineData(DecisionReadRefusalReason.FromEventProperty)]
    [InlineData(DecisionReadRefusalReason.NotEventSourceKeyed)]
    [InlineData(DecisionReadRefusalReason.NoEventTypes)]
    [InlineData(DecisionReadRefusalReason.UnsupportedEventTypeId)]
    [InlineData(DecisionReadRefusalReason.KeyConversion)]
    public async Task should_refuse_unsafe_shapes_without_rpc(DecisionReadRefusalReason reason)
    {
        var modelType = typeof(Model);
        switch (reason)
        {
            case DecisionReadRefusalReason.Reducer:
#pragma warning disable CA2263 // Verify the non-generic lookup used by DecisionReads.Assess.
                _reducers.HasFor(typeof(Model)).Returns(true);
#pragma warning restore CA2263
                break;
            case DecisionReadRefusalReason.AmbiguousProjection:
                SetDefinitions(_definition, _definition);
                break;
            case DecisionReadRefusalReason.NotEventLog:
                _definition.EventSequenceId = "inbox";
                break;
            case DecisionReadRefusalReason.Join:
                _definition.Join[new() { Id = "joined" }] = new();
                break;
            case DecisionReadRefusalReason.Hierarchy:
                _definition.Children["child"] = new();
                break;
            case DecisionReadRefusalReason.OpenEndedEventTypes:
                _definition.SubscribesToAllEvents = true;
                break;
            case DecisionReadRefusalReason.Derivatives:
                _definition.FromEvery.Add(new());
                break;
            case DecisionReadRefusalReason.FromEventProperty:
                _definition.FromEventProperty = new();
                break;
            case DecisionReadRefusalReason.NotEventSourceKeyed:
                _definition.From.First().Value.Key = "$value(id)";
                break;
            case DecisionReadRefusalReason.NoEventTypes:
                _definition.From.Clear();
                break;
            case DecisionReadRefusalReason.UnsupportedEventTypeId:
                _definition.From.Clear();
                _definition.From[new() { Id = "a,b" }] = new();
                break;
            case DecisionReadRefusalReason.KeyConversion:
                modelType = typeof(NumericModel);
                _definition.ReadModel = modelType.GetReadModelIdentifier();
                break;
        }

        var admission = modelType == typeof(Model) ? _reader.Admit<Model>() : _reader.Admit<NumericModel>();
        admission.Reason.ShouldEqual(reason);
        var error = modelType == typeof(Model)
            ? await Record.ExceptionAsync(() => _reader.GetDetached<Model>("source"))
            : await Record.ExceptionAsync(() => _reader.GetDetached<NumericModel>("source"));
        ((DecisionReadRefused)error).Reason.ShouldEqual(reason);
        _readModels.ReceivedCalls().ShouldBeEmpty();
        _projectionService.ReceivedCalls().ShouldBeEmpty();
        _sequences.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("source#alias")]
    [InlineData("source ")]
    [InlineData("*")]
    public async Task should_refuse_invalid_keys_before_any_rpc(string key)
    {
        var error = await Record.ExceptionAsync(() => _reader.GetDetached<Model>(key));
        ((DecisionReadRefused)error).Reason.ShouldEqual(DecisionReadRefusalReason.InvalidKey);
        _readModels.ReceivedCalls().ShouldBeEmpty();
        _sequences.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task should_refuse_a_noncanonical_guid_before_any_rpc()
    {
        _definition.ReadModel = typeof(GuidModel).GetReadModelIdentifier();
        var error = await Record.ExceptionAsync(() => _reader.GetDetached<GuidModel>(Guid.NewGuid().ToString("D").ToUpperInvariant()));
        ((DecisionReadRefused)error).Reason.ShouldEqual(DecisionReadRefusalReason.InvalidKey);
        _readModels.ReceivedCalls().ShouldBeEmpty();
        _sequences.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public void should_refuse_a_model_without_a_key_property()
    {
        _definition.ReadModel = typeof(NoKeyModel).GetReadModelIdentifier();
        _reader.Admit<NoKeyModel>().Reason.ShouldEqual(DecisionReadRefusalReason.KeyConversion);
        _readModels.ReceivedCalls().ShouldBeEmpty();
    }

    protected record GuidModel(Guid Id);
}
