// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Integration.for_PerProcessActivation;

public sealed class PerProcessDiscovery : ICanProvideAssembliesForDiscovery
{
    public IEnumerable<Assembly> Assemblies => [typeof(PerProcessDiscovery).Assembly];
    public IEnumerable<Type> DefinedTypes => [typeof(PerProcessStartupHandler), typeof(ProcessStarted)];
    public void Initialize() { }
}
