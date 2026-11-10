// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace Cratis.Chronicle.for_ChronicleClientWebApplicationBuilderExtensions;

public class when_no_event_store_is_configured : given.a_web_builder
{
    void Because()
    {
        _builder.AddCratisChronicle();
        ResolveOptions();
    }

    [Fact] void should_default_to_the_mongo_safe_entry_assembly_name() => _options.EventStore.Value.ShouldEqual(Assembly.GetEntryAssembly()!.GetName().Name!.Replace('.', '-')[..32]);
}
