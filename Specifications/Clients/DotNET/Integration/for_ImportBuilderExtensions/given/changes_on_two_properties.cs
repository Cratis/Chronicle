// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Integration.for_ImportBuilderExtensions.given;

public class changes_on_two_properties : no_changes
{
    void Establish()
    {
        modified_model = new Model(43, "Forty Three", "Three");
        original_model = new Model(42, "Forty Two", "Two");

        changeset.Add(new PropertiesChanged<Model>(modified_model,
        [
                new PropertyDifference(
                    new(nameof(Model.SomeInteger)),
                    original_model,
                    modified_model),

                new PropertyDifference(
                    new(nameof(Model.SomeString)),
                    original_model,
                    modified_model)
        ]));
    }
}
