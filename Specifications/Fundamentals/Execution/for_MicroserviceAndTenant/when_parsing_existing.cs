// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Execution.for_MicroserviceAndTenant;

public class when_parsing_existing : Specification
{
    const string tenant_id = "0c2adff8-7ac6-4998-bcec-c58c18d45f8f";
    const string microservice_id = "7a28c4fa-cfd4-405e-9873-753bab4fd2e3";
    const string combined = $"{microservice_id}+{tenant_id}";

    MicroserviceAndTenant result;

    void Because() => result = MicroserviceAndTenant.Parse(combined);

    [Fact] void should_hold_correct_microservice_id() => result.MicroserviceId.ToString().ShouldEqual(microservice_id);
    [Fact] void should_hold_correct_tenant_id() => result.TenantId.ToString().ShouldEqual(tenant_id);
}
