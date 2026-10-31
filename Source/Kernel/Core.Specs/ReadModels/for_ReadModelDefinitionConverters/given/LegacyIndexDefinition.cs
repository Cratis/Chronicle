// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.ReadModels.for_ReadModelDefinitionConverters.given;

[ProtoContract]
public class LegacyIndexDefinition
{
    [ProtoMember(1)]
    public string PropertyPath { get; set; } = string.Empty;
}
