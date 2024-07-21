// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Models;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.for_Projection.given;

public class a_projection : Specification
{
    protected Projection projection;

    void Establish()
    {
        projection = new Projection(
            "0b7325dd-7a25-4681-9ab7-c387a6073547",
            new ExpandoObject(),
            string.Empty,
            string.Empty,
            new Model(string.Empty, new JsonSchema()),
            true,
            []);
    }
}
