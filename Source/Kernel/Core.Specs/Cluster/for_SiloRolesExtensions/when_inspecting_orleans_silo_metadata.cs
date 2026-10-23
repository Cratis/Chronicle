// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Orleans.Runtime.MembershipService.SiloMetadata;

namespace Cratis.Chronicle.Cluster.for_SiloRolesExtensions;

public class when_inspecting_orleans_silo_metadata : Specification
{
    MethodInfo? _method;

    void Because() => _method = typeof(SiloMetadata).GetMethod(
        "AddMetadata",
        BindingFlags.Instance | BindingFlags.NonPublic,
        [typeof(IEnumerable<KeyValuePair<string, string>>)]);

    [Fact] void should_have_the_internal_add_metadata_method() => _method.ShouldNotBeNull();
}
