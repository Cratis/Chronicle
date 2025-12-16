// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.for_PropertyExtensions.when_converting_to_mongo_db;

public class with_array_and_child_property : Specification
{
    PropertyPath _propertyPath;
    string _result;

    void Establish() => _propertyPath = new PropertyPath("[Configurations].ConfigurationId");

    void Because() => _result = _propertyPath.ToMongoDB();

    [Fact] void should_remove_brackets_and_preserve_casing() => _result.ShouldEqual("Configurations.ConfigurationId");
}
