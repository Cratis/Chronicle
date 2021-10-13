// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Reflection;
using Cratis.Types;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjections"/>.
    /// </summary>
    public class Projections : IProjections
    {
        static class ProjectionDefinitionCreator<TModel>
        {
            public static ProjectionDefinition CreateAndDefine(Type type, ProjectionId identifier, IEventTypes eventTypes)
            {
                var instance = (Activator.CreateInstance(type) as IProjectionFor<TModel>)!;
                var builder = new ProjectionBuilderFor<TModel>(identifier, eventTypes);
                instance.Define(builder);
                return builder.Build();
            }
        }

        protected readonly IEnumerable<ProjectionDefinition> _projections;

        /// <summary>
        /// Initializes a new instance of the <see cref="Projections"/> class.
        /// </summary>
        /// <param name="eventTypes"><see cref="IEventTypes"/> to use.</param>
        /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
        public Projections(IEventTypes eventTypes, ITypes types)
        {
            _projections = types.All
                    .Where(_ => _.HasAttribute<ProjectionAttribute>() && _.HasInterface(typeof(IProjectionFor<>)))
                    .Select(_ =>
                    {
                        var projection = _.GetCustomAttribute<ProjectionAttribute>()!;
                        var modelType = _.GetInterface(typeof(IProjectionFor<>).Name)!.GetGenericArguments()[0]!;
                        var creatorType = typeof(ProjectionDefinitionCreator<>).MakeGenericType(modelType);
                        var method = creatorType.GetMethod(nameof(ProjectionDefinitionCreator<object>.CreateAndDefine), BindingFlags.Public | BindingFlags.Static)!;
                        return (method.Invoke(null, new object[] { _, projection.Identifier, eventTypes }) as ProjectionDefinition)!;
                    }).ToArray();
        }

        /// <inheritdoc/>
        public virtual void StartAll()
        {
        }
    }
}
