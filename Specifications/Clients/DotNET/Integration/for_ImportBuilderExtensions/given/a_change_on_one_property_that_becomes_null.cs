// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Changes;

namespace Aksio.Cratis.Integration.for_ImportBuilderExtensions.given;

public class a_change_on_one_property_that_becomes_null : no_changes
{
    void Establish()
    {
        original_model = new Model(42, "Forty Two", "Two");
        modified_model = new Model(42, null, null);

        changeset.Add(new PropertiesChanged<Model>(modified_model, new[]
        {
                new PropertyDifference(
                    new(nameof(Model.SomeString)),
                    original_model.SomeString,
                    modified_model.SomeString)
        }));
    }
}
