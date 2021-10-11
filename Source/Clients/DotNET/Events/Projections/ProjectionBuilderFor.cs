// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// /// Represents an implementation of <see cref="IProjectionBuilderFor{TModel}"/>.
    /// </summary>
    /// <typeparam name="TModel">Type of model.</typeparam>
    public class ProjectionBuilderFor<TModel> : IProjectionBuilderFor<TModel>
    {
        readonly ProjectionId _identifier;
        string _modelName;
        readonly Dictionary<string, FromDefinition> _fromDefintions = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectionBuilderFor{TModel}"/> class.
        /// </summary>
        /// <param name="identifier">The unique identifier for the projection.</param>
        public ProjectionBuilderFor(ProjectionId identifier)
        {
            _identifier = identifier;
            _modelName = typeof(TModel).Name;
        }

        /// <inheritdoc/>
        public IProjectionBuilderFor<TModel> ModelName(string modelName)
        {
            _modelName = modelName;
            return this;
        }

        /// <inheritdoc/>
        public IProjectionBuilderFor<TModel> From<TEvent>(Action<IFromBuilder<TModel, TEvent>> builderCallback)
        {
            var builder = new FromBuilder<TModel, TEvent>();
            builderCallback(builder);
            return this;
        }

        /// <inheritdoc/>
        public ProjectionDefinition Build()
        {
            return new ProjectionDefinition(
                _identifier,
                new ModelDefinition(_modelName),
                _fromDefintions);
        }
    }
}
