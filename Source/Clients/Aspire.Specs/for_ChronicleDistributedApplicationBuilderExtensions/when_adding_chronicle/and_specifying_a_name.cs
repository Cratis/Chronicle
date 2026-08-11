// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire.for_ChronicleDistributedApplicationBuilderExtensions.when_adding_chronicle;

public class and_specifying_a_name : given.a_distributed_application_builder
{
    const string Name = "event-store";

    IResourceBuilder<ChronicleResource> _result;

    void Because() => _result = _builder.AddCratisChronicle(Name);

    [Fact] void should_name_the_resource_as_specified() => _result.Resource.Name.ShouldEqual(Name);
}
