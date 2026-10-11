// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.given;

public class generated_schema_code : generated_order_code
{
    protected const string Subscription = "projection Order => Proof.Pdl.Order\n  from OrderPlaced";

    protected void UseSchema(string json) => _readModelDefinition.Schemas[ReadModelGeneration.First] = JsonSchema.FromJson(json);
}
