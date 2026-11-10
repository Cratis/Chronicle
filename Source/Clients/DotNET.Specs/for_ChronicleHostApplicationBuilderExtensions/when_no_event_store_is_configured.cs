// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.for_ChronicleHostApplicationBuilderExtensions;

public class when_no_event_store_is_configured : given.a_host_builder
{
    void Because()
    {
        _builder.AddCratisChronicle();
        ResolveOptions();
    }

    [Fact] void should_default_to_the_normalized_entry_assembly_name() => _options.EventStore.Value.ShouldEqual(Assembly.GetEntryAssembly()!.GetName().Name!.Replace('.', '-'));
}
