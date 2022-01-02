// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Store.MongoDB.Reminders;
using Microsoft.Extensions.DependencyInjection;

namespace Orleans.Hosting
{
    /// <summary>
    /// Extensions for <see cref="ISiloBuilder"/>.
    /// </summary>
    public static class SiloBuilderReminderExtensions
    {
        /// <summary>
        /// Use MongoDB for persisting Grain reminders.
        /// </summary>
        /// <param name="builder"><see cref="ISiloBuilder"/> to extend.</param>
        /// <returns><see cref="ISiloBuilder"/> for continuation.</returns>
        public static ISiloBuilder UseMongoDBReminderService(this ISiloBuilder builder)
        {
            builder.ConfigureServices(services => services.AddSingleton<IReminderTable, MongoDBReminderTable>());
            return builder;
        }
    }
}
