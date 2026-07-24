// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelDiffer.given;

public class a_differ : Specification
{
    protected MaterializedReadModelDiffer _differ;

    void Establish() => _differ = new MaterializedReadModelDiffer(new JsonSerializerOptions());
}
