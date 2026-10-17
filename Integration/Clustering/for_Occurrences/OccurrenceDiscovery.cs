// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Integration.Clustering.for_Occurrences;

public sealed class OccurrenceDiscovery(params Type[] types) : ICanProvideAssembliesForDiscovery
{
    public IEnumerable<Assembly> Assemblies => types.Select(type => type.Assembly).Distinct();
    public IEnumerable<Type> DefinedTypes => types;
    public void Initialize() { }
}
