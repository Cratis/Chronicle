// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.for_PropertyExtensions.when_converting_to_mongo_db;

public class with_simple_property : Specification
{
    PropertyPath _propertyPath;
    string _result;

    void Establish() => _propertyPath = new PropertyPath("Name");

    void Because() => _result = _propertyPath.ToMongoDB();

    [Fact] void should_return_same_cased_property() => _result.ShouldEqual("Name");
}
