// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Types.for_ContractToImplementorsMap.given;

public class an_empty_map : Specification
{
    protected ContractToImplementorsMap map;

    void Establish() => map = new();
}
