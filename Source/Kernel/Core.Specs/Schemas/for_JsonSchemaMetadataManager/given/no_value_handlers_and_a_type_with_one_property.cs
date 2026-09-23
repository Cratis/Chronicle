// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.given;

public class no_value_handlers_and_a_type_with_one_property : a_type_with_one_property
{
    protected JsonSchemaMetadataManager manager;

    void Establish() => manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(), NullLogger<JsonSchemaMetadataManager>.Instance);
}
