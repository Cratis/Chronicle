// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Autofac;

namespace Cratis.Extensions.MongoDB
{
    /// <summary>
    /// Represents a <see cref="Module"/> for setting up defaults and bindings for MongoDB
    /// </summary>
    public class MongoDBModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            MongoDBDefaults.Initialize();
        }
    }
}
