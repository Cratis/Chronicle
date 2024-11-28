// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Integration.for_ImportBuilderExtensions.given;

public class changes_on_two_properties : no_changes
{
    void Establish()
    {
        _modifiedModel = new Model(43, "Forty Three", "Three");
        _originalModel = new Model(42, "Forty Two", "Two");

        _changeset.Add(new PropertiesChanged<Model>(_modifiedModel,
        [
                new PropertyDifference(
                    new(nameof(Model.SomeInteger)),
                    _originalModel,
                    _modifiedModel),

                new PropertyDifference(
                    new(nameof(Model.SomeString)),
                    _originalModel,
                    _modifiedModel)
        ]));
    }
}
