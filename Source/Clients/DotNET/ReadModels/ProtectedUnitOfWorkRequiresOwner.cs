// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>Thrown when an application attempts to complete a protected unit of work without its owner.</summary>
public class ProtectedUnitOfWorkRequiresOwner() : Exception("Only the owner can commit a protected unit of work.");
