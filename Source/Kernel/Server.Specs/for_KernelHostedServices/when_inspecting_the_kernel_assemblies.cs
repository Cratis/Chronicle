// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Setup;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.for_KernelHostedServices;

/// <summary>
/// Hosted services start before the listeners bind, and the silo is deferred until after they have all started. A
/// hosted service that calls a grain therefore waits for a silo that cannot start until it returns, and the port never
/// binds (#4040). Work that needs grains belongs to the silo lifecycle instead.
/// </summary>
public class when_inspecting_the_kernel_assemblies : Specification
{
    static readonly Type[] _grainAccess = [typeof(IGrainFactory), typeof(IClusterClient)];

    string[] _offenders;

    void Because() => _offenders = new[] { typeof(Kernel).Assembly, typeof(IBusySilos).Assembly }
        .SelectMany(TypesOf)
        .Where(_ => _ is { IsClass: true, IsAbstract: false } && typeof(IHostedService).IsAssignableFrom(_) && TakesGrainAccess(_))
        .Select(_ => _.FullName!)
        .ToArray();

    [Fact] void should_have_no_hosted_service_that_takes_grain_access() => _offenders.ShouldBeEmpty();

    static bool TakesGrainAccess(Type type) => type
        .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .Any(constructor => constructor.GetParameters().Any(parameter => _grainAccess.Any(grains => grains.IsAssignableFrom(parameter.ParameterType))));

    static IEnumerable<Type> TypesOf(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }
}
