// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectHandlers;

/// <summary>
/// The built-in handlers are public classes, so compatibility requires the previous members to remain on the
/// concrete types. Keeping only default interface methods does not preserve source or binary calls bound to a class.
/// </summary>
public class when_inspecting_the_previous_public_surface : Specification
{
    readonly Type[] _handlersWithEventTypesConstructor =
    [
        typeof(EventResultHandler),
        typeof(EventsResultHandler),
        typeof(MixedSideEffectsResultHandler)
    ];

    readonly Type[] _handlersWithEventStorelessCanHandle =
    [
        typeof(EventResultHandler),
        typeof(EventsResultHandler),
        typeof(MixedSideEffectsResultHandler),
        typeof(EventForEventSourceIdResultHandler),
        typeof(EventsForEventSourceIdResultHandler),
        typeof(ReactorSideEffectHandlers)
    ];

    Type[] _handlersMissingTheConstructor;
    Type[] _handlersMissingCanHandle;
    Type[] _handlersMissingSingletonMetadata;
    System.Reflection.MemberInfo[] _obsoleteMembers;

    void Because()
    {
        _handlersMissingTheConstructor = _handlersWithEventTypesConstructor
            .Where(_ => _.GetConstructor([typeof(IEventTypes)]) is null)
            .ToArray();
        _handlersMissingCanHandle = _handlersWithEventStorelessCanHandle
            .Where(_ => _.GetMethod(
                nameof(IReactorSideEffectHandler.CanHandle),
                [typeof(ReactorContext), typeof(object)]) is null)
            .ToArray();
        _handlersMissingSingletonMetadata = _handlersWithEventStorelessCanHandle
            .Where(type => !Attribute.IsDefined(type, typeof(SingletonAttribute)))
            .ToArray();

        _obsoleteMembers = _handlersWithEventTypesConstructor
            .Select(type => (System.Reflection.MemberInfo)type.GetConstructor([typeof(IEventTypes)])!)
            .Concat(_handlersWithEventStorelessCanHandle.Select(type =>
                (System.Reflection.MemberInfo)type.GetMethod(
                    nameof(IReactorSideEffectHandler.CanHandle),
                    [typeof(ReactorContext), typeof(object)])!))
            .Concat(
            [
                typeof(IReactorSideEffectHandler).GetMethod(
                    nameof(IReactorSideEffectHandler.CanHandle),
                    [typeof(ReactorContext), typeof(object)])!,
                typeof(IReactorSideEffectHandlers).GetMethod(
                    nameof(IReactorSideEffectHandlers.CanHandle),
                    [typeof(ReactorContext), typeof(object)])!
            ])
            .Where(member => member.IsDefined(typeof(ObsoleteAttribute), inherit: false))
            .ToArray();
    }

    [Fact] void should_keep_every_event_types_constructor() => _handlersMissingTheConstructor.ShouldBeEmpty();
    [Fact] void should_keep_every_concrete_event_storeless_can_handle() => _handlersMissingCanHandle.ShouldBeEmpty();
    [Fact] void should_keep_the_published_singleton_metadata() => _handlersMissingSingletonMetadata.ShouldBeEmpty();
    [Fact] void should_keep_the_previous_surface_non_obsolete() => _obsoleteMembers.ShouldBeEmpty();
}
