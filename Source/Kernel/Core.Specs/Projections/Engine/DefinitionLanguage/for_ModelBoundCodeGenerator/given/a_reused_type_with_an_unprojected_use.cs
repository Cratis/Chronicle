// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ModelBoundCodeGenerator.given;

public class a_reused_type_with_an_unprojected_use : CodeGeneration.given.generated_schema_code
{
    protected const string Declaration = """
        projection Order => Proof.Pdl.Order
          from OrderPlaced
          nested BillingAddress
            from OrderPlaced
              DisplayName = CustomerName
        """;

    protected Contracts.Projections.ProjectionDefinition _modelBound;

    protected void UseAddresses(bool unprojectedFirst)
    {
        const string projected = "\"BillingAddress\": { \"$ref\": \"#/$defs/Address\" }";
        const string unprojected = "\"ArchivedAddress\": { \"$ref\": \"#/$defs/Address\" }";
        var addresses = unprojectedFirst ? $"{unprojected}, {projected}" : $"{projected}, {unprojected}";
        UseSchema($$"""
            { "title": "Order", "type": "object", "properties": {
              "Id": { "type": "string" }, {{addresses}}
            }, "$defs": { "Address": { "title": "Address", "type": "object", "properties": {
              "DisplayName": { "type": "string" }
            } } } }
            """);
    }
}
