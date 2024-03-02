// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reflection;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Reflection;
using AutoMapper;

namespace Aksio.Cratis.Integration;

/// <summary>
/// Extension methods for building on the <see cref="IImportBuilderFor{TModel, TExternalModel}"/>.
/// </summary>
public static class ImportBuilderExtensions
{
    /// <summary>
    /// Filter down to when a model already exists.
    /// </summary>
    /// <param name="builder"><see cref="IImportBuilderFor{TModel, TExternalModel}"/> to build the filter for.</param>
    /// <typeparam name="TModel">Type of model.</typeparam>
    /// <typeparam name="TExternalModel">Type of external model.</typeparam>
    /// <returns>Observable for chaining.</returns>
    public static IObservable<ImportContext<TModel, TExternalModel>> WhenModelExists<TModel, TExternalModel>(this IImportBuilderFor<TModel, TExternalModel> builder)
    {
        return builder.Where(_ => _.InitialProjectionResult.ProjectedEventsCount > 0 && _.InitialProjectionResult.AffectedProperties.Any());
    }

    /// <summary>
    /// Filter down to when a model does not exist.
    /// </summary>
    /// <param name="builder"><see cref="IImportBuilderFor{TModel, TExternalModel}"/> to build the filter for.</param>
    /// <typeparam name="TModel">Type of model.</typeparam>
    /// <typeparam name="TExternalModel">Type of external model.</typeparam>
    /// <returns>Observable for chaining.</returns>
    public static IObservable<ImportContext<TModel, TExternalModel>> WhenModelDoesNotExist<TModel, TExternalModel>(this IImportBuilderFor<TModel, TExternalModel> builder)
    {
        return builder.Where(_ => _.InitialProjectionResult.ProjectedEventsCount == 0 && !_.InitialProjectionResult.AffectedProperties.Any());
    }

    /// <summary>
    /// Filter down to when specific properties on a model are set.
    /// </summary>
    /// <param name="builder"><see cref="IImportBuilderFor{TModel, TExternalModel}"/> to build the filter for.</param>
    /// <param name="properties">Properties as expressions to look for if was set on model.</param>
    /// <typeparam name="TModel">Type of model.</typeparam>
    /// <typeparam name="TExternalModel">Type of external model.</typeparam>
    /// <returns>Observable for chaining.</returns>
    public static IObservable<ImportContext<TModel, TExternalModel>> WhenModelPropertiesAreSet<TModel, TExternalModel>(this IImportBuilderFor<TModel, TExternalModel> builder, params Expression<Func<TModel, object>>[] properties)
    {
        var propertyPaths = properties.Select(_ => _.GetPropertyPath()).ToArray();
        return builder.Where(_ => _.InitialProjectionResult.AffectedProperties.Any(_ => propertyPaths.Contains(_)));
    }

    /// <summary>
    /// Filter down to when specific properties on a model are set.
    /// </summary>
    /// <param name="builder"><see cref="IImportBuilderFor{TModel, TExternalModel}"/> to build the filter for.</param>
    /// <param name="properties">Properties as expressions to look for if was set on model.</param>
    /// <typeparam name="TModel">Type of model.</typeparam>
    /// <typeparam name="TExternalModel">Type of external model.</typeparam>
    /// <returns>Observable for chaining.</returns>
    public static IObservable<ImportContext<TModel, TExternalModel>> WhenModelPropertiesAreNotSet<TModel, TExternalModel>(this IImportBuilderFor<TModel, TExternalModel> builder, params Expression<Func<TModel, object>>[] properties)
    {
        var propertyPaths = properties.Select(_ => _.GetPropertyPath()).ToArray();
        return builder.Where(_ => !_.InitialProjectionResult.AffectedProperties.Any(_ => propertyPaths.Contains(_)));
    }

    /// <summary>
    /// Filter down to when one of the properties defined changes.
    /// </summary>
    /// <param name="context">Observable of the <see cref="ImportContext{TModel, TExternalModel}"/>.</param>
    /// <param name="properties">Properties as expressions to look for changes on.</param>
    /// <typeparam name="TModel">Type of model.</typeparam>
    /// <typeparam name="TExternalModel">Type of external model.</typeparam>
    /// <returns>Observable for chaining.</returns>
    public static IObservable<ImportContext<TModel, TExternalModel>> WithProperties<TModel, TExternalModel>(this IObservable<ImportContext<TModel, TExternalModel>> context, params Expression<Func<TModel, object>>[] properties)
    {
        var propertyPaths = properties.Select(_ => _.GetPropertyPath()).ToArray();

        return context.Where(_ =>
        {
            var changes = _.Changeset.Changes.Where(_ => _ is PropertiesChanged<TModel>).Select(_ => _ as PropertiesChanged<TModel>);
            return changes.Any(_ => _!.Differences.Any(d => d.PropertyChanged(propertyPaths)));
        });
    }

    /// <summary>
    /// Filter down to when one of the properties defined changes from a value to a null value.
    /// </summary>
    /// <param name="context">Observable of the <see cref="ImportContext{TModel, TExternalModel}"/>.</param>
    /// <param name="properties">Properties as expressions to look for changes on.</param>
    /// <typeparam name="TModel">Type of model.</typeparam>
    /// <typeparam name="TExternalModel">Type of external model.</typeparam>
    /// <returns>Observable for chaining.</returns>
    public static IObservable<ImportContext<TModel, TExternalModel>> WithPropertiesBecomingNull<TModel, TExternalModel>(this IObservable<ImportContext<TModel, TExternalModel>> context, params Expression<Func<TModel, object>>[] properties)
    {
        var propertyPaths = properties.Select(_ => _.GetPropertyPath()).ToArray();

        return context.Where(_ =>
        {
            var changes = _.Changeset.Changes.Where(_ => _ is PropertiesChanged<TModel>).Select(_ => _ as PropertiesChanged<TModel>);
            return changes.Any(_ => _!.Differences.Any(_ => _.Original is not null && _.Changed is null && _.PropertyChanged(propertyPaths)));
        });
    }

    /// <summary>
    /// Append an event by automatically mapping property names matching from the model onto the event.
    /// </summary>
    /// <param name="context">Observable of the <see cref="ImportContext{TModel, TExternalModel}"/>.</param>
    /// <param name="validFromCallback">Callback for providing valid from.</param>
    /// <typeparam name="TModel">Type of model.</typeparam>
    /// <typeparam name="TExternalModel">Type of external model.</typeparam>
    /// <typeparam name="TEvent">Type of event to append.</typeparam>
    /// <returns>Observable for chaining.</returns>
    public static IObservable<ImportContext<TModel, TExternalModel>> AppendEvent<TModel, TExternalModel, TEvent>(
        this IObservable<ImportContext<TModel, TExternalModel>> context,
        Func<ImportContext<TModel, TExternalModel>, DateTimeOffset?> validFromCallback)
    {
        context.Subscribe(_ =>
        {
            foreach (var change in _.Changeset.Changes.Where(_ => _ is PropertiesChanged<TModel>).Select(_ => _ as PropertiesChanged<TModel>))
            {
                try
                {
                    var mapper = ModelToEventMapperFor<TModel, TEvent>.Mapper;
                    _.Events.Add(mapper.Map<TEvent>(((TModel)change!.State)!)!, validFromCallback(_));
                }
                catch (TypeInitializationException ex)
                {
                    throw ex.InnerException!;
                }
            }
        });

        return context;
    }

    /// <summary>
    /// Append an event by automatically mapping property names matching from the model onto the event.
    /// </summary>
    /// <param name="context">Observable of the <see cref="ImportContext{TModel, TExternalModel}"/>.</param>
    /// <param name="validFrom">Optional date and time for when the event is valid from. </param>
    /// <typeparam name="TModel">Type of model.</typeparam>
    /// <typeparam name="TExternalModel">Type of external model.</typeparam>
    /// <typeparam name="TEvent">Type of event to append.</typeparam>
    /// <returns>Observable for chaining.</returns>
    public static IObservable<ImportContext<TModel, TExternalModel>> AppendEvent<TModel, TExternalModel, TEvent>(
        this IObservable<ImportContext<TModel, TExternalModel>> context,
        DateTimeOffset? validFrom = default)
    {
        context.Subscribe(_ =>
        {
            foreach (var change in _.Changeset.Changes.Where(_ => _ is PropertiesChanged<TModel>).Select(_ => _ as PropertiesChanged<TModel>))
            {
                try
                {
                    var mapper = ModelToEventMapperFor<TModel, TEvent>.Mapper;
                    _.Events.Add(mapper.Map<TEvent>(((TModel)change!.State)!)!, validFrom);
                }
                catch (TypeInitializationException ex)
                {
                    throw ex.InnerException!;
                }
            }
        });

        return context;
    }

    /// <summary>
    /// Append an event through calling a callback that will be responsible for creating an instance of the event.
    /// </summary>
    /// <param name="context">Observable of the <see cref="ImportContext{TModel, TExternalModel}"/>.</param>
    /// <param name="creationCallback">Callback for creating the instance.</param>
    /// <param name="validFrom">Optional date and time for when the event is valid from. </param>
    /// <typeparam name="TModel">Type of model.</typeparam>
    /// <typeparam name="TExternalModel">Type of external model.</typeparam>
    /// <typeparam name="TEvent">Type of event to append.</typeparam>
    /// <returns>Observable for chaining.</returns>
    public static IObservable<ImportContext<TModel, TExternalModel>> AppendEvent<TModel, TExternalModel, TEvent>(
        this IObservable<ImportContext<TModel, TExternalModel>> context,
        Func<ImportContext<TModel, TExternalModel>, TEvent> creationCallback,
        DateTimeOffset? validFrom = default)
    {
        context.Subscribe(_ => _.Events.Add(creationCallback(_)!, validFrom));
        return context;
    }

    /// <summary>
    /// Append an event through calling a callback that will be responsible for creating an instance of the event.
    /// </summary>
    /// <param name="context">Observable of the <see cref="ImportContext{TModel, TExternalModel}"/>.</param>
    /// <param name="creationCallback">Callback for creating the instance.</param>
    /// <param name="validFromCallback">Callback for providing valid from.</param>
    /// <typeparam name="TModel">Type of model.</typeparam>
    /// <typeparam name="TExternalModel">Type of external model.</typeparam>
    /// <typeparam name="TEvent">Type of event to append.</typeparam>
    /// <returns>Observable for chaining.</returns>
    public static IObservable<ImportContext<TModel, TExternalModel>> AppendEvent<TModel, TExternalModel, TEvent>(
        this IObservable<ImportContext<TModel, TExternalModel>> context,
        Func<ImportContext<TModel, TExternalModel>, TEvent> creationCallback,
        Func<ImportContext<TModel, TExternalModel>, DateTimeOffset?> validFromCallback)
    {
        context.Subscribe(_ => _.Events.Add(creationCallback(_)!, validFromCallback(_)));
        return context;
    }

    /// <summary>
    /// Checks if the property difference is an exact match or a nested property of any of the propertyPaths.
    /// </summary>
    /// <param name="propertyDifference">The changed property.</param>
    /// /// <param name="propertyPaths">The detection paths to check against.</param>
    /// <returns>True if the changed property is an exact match or a child node of the configured property path.</returns>
    static bool PropertyChanged(this PropertyDifference propertyDifference, PropertyPath[] propertyPaths) =>
        propertyPaths.Any(
            propertyPath =>
            {
                var changedPath = propertyDifference.PropertyPath.Path;
                var detectionPath = propertyPath.Path;

                // Detect exact path change.
                if (changedPath.Equals(detectionPath))
                {
                    return true;
                }

                // Detect nested changes.
                if (changedPath.Length > detectionPath.Length && changedPath[detectionPath.Length] == '.')
                {
                    return changedPath.StartsWith(detectionPath);
                }

                return false;
            });

    static class ModelToEventMapperFor<TModel, TEvent>
    {
        public static IMapper Mapper;

        static ModelToEventMapperFor()
        {
            var eventProperties = typeof(TEvent).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var modelProperties = typeof(TModel).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var configuration = new MapperConfiguration(cfg =>
            {
                var map = cfg.CreateMap<TModel, TEvent>();
                foreach (var eventProperty in eventProperties)
                {
                    if (!modelProperties.Any(_ => _.Name == eventProperty.Name))
                    {
                        throw new MissingExpectedEventPropertyOnModel(typeof(TEvent), typeof(TModel), eventProperty.Name);
                    }

                    map.ForMember(eventProperty.Name, _ => _.MapFrom(eventProperty.Name));
                    map.ForCtorParam(eventProperty.Name, _ => _.MapFrom(eventProperty.Name));
                }
            });

            Mapper = configuration.CreateMapper();
        }
    }
}
