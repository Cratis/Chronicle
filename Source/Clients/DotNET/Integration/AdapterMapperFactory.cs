// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AutoMapper;

namespace Aksio.Cratis.Integration;

/// <summary>
/// Represents an implementation of <see cref="IAdapterMapperFactory"/>.
/// </summary>
public class AdapterMapperFactory : IAdapterMapperFactory
{
    /// <inheritdoc/>
    public IMapper CreateFor<TModel, TExternalModel>(IAdapterFor<TModel, TExternalModel> adapter)
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.ShouldUseConstructor = ci =>
            {
                var parameters = ci.GetParameters();
                return !ci.IsPrivate && !(parameters.Length == 1 && parameters[0].ParameterType.Equals(ci.DeclaringType));
            };
            cfg.ShouldMapMethod = mi => false;
            cfg.ShouldMapField = fi => !fi.IsPrivate;
            cfg.AllowNullDestinationValues = true;
            var mapping = cfg.CreateMap<TExternalModel, TModel>();
            mapping = mapping.DisableCtorValidation();
            adapter.DefineImportMapping(mapping!);
        });
        return configuration.CreateMapper();
    }
}
