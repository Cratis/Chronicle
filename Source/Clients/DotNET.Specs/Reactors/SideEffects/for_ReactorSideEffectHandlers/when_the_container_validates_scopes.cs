// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectHandlers;

/// <summary>
/// <see cref="IEventTypes"/> is registered scoped, because the event type registry belongs to the event store —
/// and therefore the namespace — the resolving scope named. Anything Cratis's DI convention registers for the
/// process lifetime must therefore not take one in its constructor: with <c>ValidateScopes</c> on the resolution
/// is refused and the reactor's side-effect append never happens, and with it off the captured registry answers
/// for every other namespace in the host.
/// </summary>
/// <remarks>
/// Both sides of the rule are taken from the real thing rather than listed here: the scoped service types come
/// from the service collection <c>AddCratisChronicleClient</c> actually produces, and the process-lifetime types
/// come from every class in the client assembly the DI convention would register as a singleton. A type added
/// later that repeats the mistake is therefore caught without anyone remembering to name it.
/// <para>
/// Every public constructor is inspected, not only the one the type intends to be used through. The container
/// picks the greediest constructor whose parameters it can all resolve and honors neither
/// <c>[ActivatorUtilitiesConstructor]</c> nor <c>[Obsolete]</c>, so a retained compatibility constructor taking a
/// scoped service is selected in preference to the parameterless one and reintroduces the capture.
/// </para>
/// <para>
/// The check is on direct constructor parameters. A singleton reaching a scoped service through a transient
/// collaborator is a captive dependency the container also refuses, and it is not covered here.
/// </para>
/// </remarks>
public class when_the_container_validates_scopes : Specification
{
    IEnumerable<Type> _processLifetimeTypes;
    HashSet<Type> _scopedServiceTypes;
    IEnumerable<string> _typesCapturingAScopedService;

    void Establish()
    {
        _scopedServiceTypes = [.. new ServiceCollection()
            .AddCratisChronicleClient()
            .Where(_ => _.Lifetime == ServiceLifetime.Scoped)
            .Select(_ => _.ServiceType)];

        _processLifetimeTypes = typeof(IEventSerializer).Assembly
            .GetTypes()
            .Where(_ => _ is { IsClass: true, IsAbstract: false } && Attribute.IsDefined(_, typeof(SingletonAttribute)));
    }

    void Because() =>
        _typesCapturingAScopedService = _processLifetimeTypes
            .Where(type => type.GetConstructors().Any(constructor =>
                constructor.GetParameters().Any(parameter => _scopedServiceTypes.Contains(parameter.ParameterType))))
            .Select(_ => _.FullName!)
            .Order();

    [Fact] void should_have_process_lifetime_types_to_check() => _processLifetimeTypes.ShouldNotBeEmpty();
    [Fact] void should_have_scoped_services_to_check_against() => _scopedServiceTypes.ShouldContain(typeof(IEventTypes));
    [Fact] void should_find_none_capturing_a_scoped_service() => _typesCapturingAScopedService.ShouldBeEmpty();
}
