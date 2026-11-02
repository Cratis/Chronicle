// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventTypes;

namespace Cratis.Chronicle.Events.for_EventTypes.when_registering;

public class and_cross_generation_owners_differ : given.a_cross_generation_registry
{
    EventTypes _attributeRegistry;
    EventTypes _explicitRegistry;
    RegisterEventTypesRequest _capturedRequest;
    RegisterEventTypesRequest _attributeRequest;
    RegisterEventTypesRequest _explicitRequest;

    async Task Establish()
    {
        _attributeRegistry = await CreateAttributeRegistry();
        _explicitRegistry = await CreateExplicitRegistry();
        _eventTypesService.When(service => service.RegisterEventTypes(Arg.Any<RegisterEventTypesRequest>()))
            .Do(call => _capturedRequest = call.Arg<RegisterEventTypesRequest>());
    }

    async Task Because()
    {
        await _attributeRegistry.Register();
        _attributeRequest = _capturedRequest;
        await _explicitRegistry.Register();
        _explicitRequest = _capturedRequest;
    }

    [Fact] void should_send_one_attribute_registration() => _attributeRequest.Types.Count().ShouldEqual(1);
    [Fact] void should_send_the_attribute_identity() => _attributeRequest.Types.Single().Type.Id.ShouldEqual(EventId);
    [Fact] void should_send_the_latest_attribute_generation() => _attributeRequest.Types.Single().Type.Generation.ShouldEqual(2u);
    [Fact] void should_send_the_current_attribute_owner() => _attributeRequest.Types.Single().EventStore.ShouldEqual(CurrentOwner);
    [Fact] void should_send_both_attribute_schemas() => _attributeRequest.Types.Single().Generations.Count.ShouldEqual(2);
    [Fact] void should_send_the_real_previous_attribute_schema() => _attributeRequest.Types.Single().Generations.Single(generation => generation.Generation == 1).Schema.Contains(nameof(AttributePrevious.PreviousValue)).ShouldBeTrue();
    [Fact] void should_send_the_real_current_attribute_schema() => _attributeRequest.Types.Single().Generations.Single(generation => generation.Generation == 2).Schema.Contains(nameof(AttributeCurrent.CurrentValue)).ShouldBeTrue();
    [Fact] void should_send_one_explicit_registration() => _explicitRequest.Types.Count().ShouldEqual(1);
    [Fact] void should_send_the_explicit_identity() => _explicitRequest.Types.Single().Type.Id.ShouldEqual(EventId);
    [Fact] void should_send_the_latest_explicit_generation() => _explicitRequest.Types.Single().Type.Generation.ShouldEqual(2u);
    [Fact] void should_send_the_current_explicit_owner() => _explicitRequest.Types.Single().EventStore.ShouldEqual(CurrentOwner);
    [Fact] void should_send_both_explicit_schemas() => _explicitRequest.Types.Single().Generations.Count.ShouldEqual(2);
    [Fact] void should_send_the_real_previous_explicit_schema() => _explicitRequest.Types.Single().Generations.Single(generation => generation.Generation == 1).Schema.Contains(nameof(ExplicitPrevious.PreviousValue)).ShouldBeTrue();
    [Fact] void should_send_the_real_current_explicit_schema() => _explicitRequest.Types.Single().Generations.Single(generation => generation.Generation == 2).Schema.Contains(nameof(ExplicitCurrent.CurrentValue)).ShouldBeTrue();
    [Fact] void should_retain_the_explicit_previous_descriptor_owner() => _explicitRegistry.GetDescriptorFor(typeof(ExplicitPrevious)).EventStore.ShouldEqual(PreviousOwner);
    [Fact] void should_retain_the_explicit_current_descriptor_owner() => _explicitRegistry.GetDescriptorFor(typeof(ExplicitCurrent)).EventStore.ShouldEqual(CurrentOwner);
    [Fact] void should_use_a_marker_free_explicit_previous_type() => typeof(ExplicitPrevious).GetCustomAttributes(false).OfType<EventTypeAttribute>().ShouldBeEmpty();
    [Fact] void should_use_a_marker_free_explicit_current_type() => typeof(ExplicitCurrent).GetCustomAttributes(false).OfType<EventTypeAttribute>().ShouldBeEmpty();
    [Fact] void should_not_get_the_previous_explicit_owner_from_a_marker() => Attribute.IsDefined(typeof(ExplicitPrevious), typeof(EventStoreAttribute)).ShouldBeFalse();
    [Fact] void should_not_get_the_current_explicit_owner_from_a_marker() => Attribute.IsDefined(typeof(ExplicitCurrent), typeof(EventStoreAttribute)).ShouldBeFalse();
}
