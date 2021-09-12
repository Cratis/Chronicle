// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace Cratis.Types
{
    /// <summary>
    /// Represents an implementation of <see cref="ITypes"/>
    /// </summary>
    public class Types : ITypes
    {
        readonly IContractToImplementorsMap _contractToImplementorsMap = new ContractToImplementorsMap();
        readonly List<Assembly> _assemblies = new();

        /// <summary>
        /// Initializes a new instance of <see cref="Types"/>
        /// </summary>
        public Types()
        {
            All = DiscoverAllTypes();
            _contractToImplementorsMap.Feed(All);
        }

        /// <inheritdoc/>
        public IEnumerable<Assembly> Assemblies => _assemblies;

        /// <inheritdoc/>
        public IEnumerable<Type> All { get; }

        /// <inheritdoc/>
        public Type FindSingle<T>() => FindSingle(typeof(T));

        /// <inheritdoc/>
        public IEnumerable<Type> FindMultiple<T>() => FindMultiple(typeof(T));

        /// <inheritdoc/>
        public Type FindSingle(Type type)
        {
            var typesFound = _contractToImplementorsMap.GetImplementorsFor(type);
            ThrowIfMultipleTypesFound(type, typesFound);
            return typesFound.SingleOrDefault()!;
        }

        /// <inheritdoc/>
        public IEnumerable<Type> FindMultiple(Type type)
            => _contractToImplementorsMap.GetImplementorsFor(type);

        /// <inheritdoc/>
        public Type FindTypeByFullName(string fullName)
        {
            var typeFound = _contractToImplementorsMap.All.SingleOrDefault(t => t.FullName == fullName);
            ThrowIfTypeNotFound(fullName, typeFound!);
            return typeFound!;
        }

        void ThrowIfMultipleTypesFound(Type type, IEnumerable<Type> typesFound)
        {
            if (typesFound.Count() > 1)
                throw new MultipleTypesFound(type, typesFound);
        }

        void ThrowIfTypeNotFound(string fullName, Type typeFound)
        {
            if (typeFound == null) throw new UnableToResolveTypeByName(fullName);
        }

        IEnumerable<Type> DiscoverAllTypes()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            var dependencyModel = DependencyContext.Load(entryAssembly);
            var assemblies = dependencyModel.RuntimeLibraries
                                .Where(_ => _.Type.Equals("project", StringComparison.InvariantCultureIgnoreCase))
                                .Select(_ => Assembly.Load(_.Name))
                                .ToArray();
            _assemblies.AddRange(assemblies);

            var types = new List<Type>();
            foreach (var assembly in assemblies)
            {
                types.AddRange(assembly.DefinedTypes);
            }
            return types;
        }
    }
}
