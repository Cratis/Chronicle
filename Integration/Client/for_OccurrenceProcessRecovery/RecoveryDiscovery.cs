// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Integration.for_OccurrenceProcessRecovery;

public sealed class RecoveryDiscovery : ICanProvideAssembliesForDiscovery
{
    public IEnumerable<Assembly> Assemblies => [typeof(RecoveryDiscovery).Assembly];
    public IEnumerable<Type> DefinedTypes => [typeof(RecoveryWork), typeof(RecoveryEffect), typeof(RecoveryHandler)];
    public void Initialize() { }
}
