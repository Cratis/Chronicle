// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager.given;

public class a_type_with_one_property : Specification
{
    record TypeHoldingValue(int Something);
    protected const string PropertyName = "something";
    protected const int ValueInType = 42;
    protected JsonSchema _schema;
    protected JsonObject _input;

    void Establish()
    {
        _schema = JsonSchema.FromType<TypeHoldingValue>();
        _input = new JsonObject
        {
            [PropertyName] = ValueInType
        };
    }
}
