// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Compliance.for_JsonComplianceManager.given;

public class no_value_handlers_and_a_type_with_one_property : a_type_with_one_property
{
    protected JsonComplianceManager manager;

    void Establish() => manager = new(new KnownInstancesOf<IJsonCompliancePropertyValueHandler>());
}
