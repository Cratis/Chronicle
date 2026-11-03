// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// The exception that is thrown when the names of the installation have not been checked yet.
/// </summary>
public class ResourceNamesNotReady() : Exception(ResourceNames.NotReadyMessage);
