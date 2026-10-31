// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.given;

public class generated_child_join_code : generated_schema_code
{
    protected const string Declaration = """
        projection Order => Proof.Pdl.Order
          no automap
          children lines identified by lineNumber
            from LineAdded key lineNumber
              parent orderId
              sku = sku
            join product on sku
              with ProductRenamed
                sku = sku
        """;

    protected const string ChildJoinEvents = """
        [Cratis.Chronicle.Events.EventTypeAttribute]
        public record LineAdded(string orderId, int lineNumber, string sku);
        [Cratis.Chronicle.Events.EventTypeAttribute]
        public record ProductRenamed(string sku);
        """;

    void Establish() => UseSchema("""
        { "title": "Order", "type": "object", "properties": {
          "orderId": { "type": "string" },
          "lines": { "type": "array", "items": { "title": "OrderLine", "type": "object", "properties": {
            "lineNumber": { "type": "integer" }, "sku": { "type": "string" }
          } } }
        } }
        """);
}
