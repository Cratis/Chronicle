// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;
using Polly;

namespace Cratis.MongoDB.Resilience.for_MongoCollectionInterceptorSelector.when_selecting_interceptors;

public class for_sync_methods : given.an_interceptor_selector
{
    IEnumerable<MethodInfo> sync_methods;
    int intercepted_methods;

    void Establish()
    {
        sync_methods = typeof(IMongoCollection<BsonDocument>).GetMethods().Where(m => !m.ReturnType.IsAssignableTo(typeof(Task))).ToArray();
    }

    void Because() => intercepted_methods = sync_methods.Count(methodInfo =>
    {
        var interceptors = selector.SelectInterceptors(typeof(IMongoCollection<BsonDocument>), methodInfo, []);
        return interceptors.Length == 0;
    });

    [Fact] void should_have_no_mongo_collection_interceptor_for_all() => intercepted_methods.ShouldEqual(sync_methods.Count());
}
