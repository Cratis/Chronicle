// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace Aksio.Cratis.Types
{
    /// <summary>
    /// Represents an implementation of <see cref="ITypes"/>.
    /// </summary>
    public class Types : ITypes
    {
        readonly IContractToImplementorsMap _contractToImplementorsMap = new ContractToImplementorsMap();
        readonly List<Assembly> _assemblies = new();
        readonly string[] _assemblyPrefixesToInclude;

        /// <inheritdoc/>
        public IEnumerable<Assembly> Assemblies => _assemblies;

        /// <inheritdoc/>
        public IEnumerable<Assembly> ProjectReferencedAssemblies { get; private set; } = Array.Empty<Assembly>();

        /// <inheritdoc/>
        public IEnumerable<Type> All { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="Types"/>.
        /// </summary>
        /// <param name="assemblyPrefixesToInclude">Optional params of assembly prefixes to include in type discovery.</param>
        public Types(params string[] assemblyPrefixesToInclude)
        {
            _assemblyPrefixesToInclude = assemblyPrefixesToInclude ?? Array.Empty<string>();
            All = DiscoverAllTypes();
            _contractToImplementorsMap.Feed(All);
        }

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
            {
                throw new MultipleTypesFound(type, typesFound);
            }
        }

        void ThrowIfTypeNotFound(string fullName, Type typeFound)
        {
            if (typeFound == null)
            {
                throw new UnableToResolveTypeByName(fullName);
            }
        }

        IEnumerable<Type> DiscoverAllTypes()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            var dependencyModel = DependencyContext.Load(entryAssembly);
            var projectReferencedAssemblies = dependencyModel.RuntimeLibraries
                                .Where(_ => _.Type.Equals("project", StringComparison.InvariantCultureIgnoreCase))
                                .Select(_ => Assembly.Load(_.Name))
                                .ToArray();
            _assemblies.AddRange(projectReferencedAssemblies);
            ProjectReferencedAssemblies = projectReferencedAssemblies;

            var assemblies = dependencyModel.RuntimeLibraries
                                .Where(_ => _.Name.StartsWith("Cratis", StringComparison.InvariantCultureIgnoreCase) ||
                                            _assemblyPrefixesToInclude.Any(asm => _.Name.StartsWith(asm, StringComparison.InvariantCultureIgnoreCase)))
                                .Select(_ => Assembly.Load(_.Name))
                                .Distinct()
                                .ToArray();
            _assemblies.AddRange(assemblies.Where(_ => !projectReferencedAssemblies.Any(p => p == _)).Select(_ => _));

            var types = new List<Type>();
            foreach (var assembly in _assemblies)
            {
                types.AddRange(assembly.DefinedTypes);
            }
            return types;
        }
    }
}
