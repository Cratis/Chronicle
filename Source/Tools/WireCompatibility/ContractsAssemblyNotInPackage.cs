// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility;

/// <summary>
/// The exception that is thrown when a downloaded contracts package holds no contracts assembly.
/// </summary>
/// <param name="version">The version whose package was downloaded.</param>
public class ContractsAssemblyNotInPackage(string version)
    : Exception($"The Cratis.Chronicle.Contracts {version} package holds no Cratis.Chronicle.Contracts.dll for any supported target framework.");
