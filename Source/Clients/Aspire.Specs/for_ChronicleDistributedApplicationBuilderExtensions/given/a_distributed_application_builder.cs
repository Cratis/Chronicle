// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;

namespace Cratis.Chronicle.Aspire.for_ChronicleDistributedApplicationBuilderExtensions.given;

public class a_distributed_application_builder : Specification
{
    protected IDistributedApplicationBuilder _builder;

    void Establish() => _builder = DistributedApplication.CreateBuilder([]);
}
