// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering.for_SiloSupervisor;

/// <summary>
/// A two-silo cluster of its own for specifications that terminate a silo, so the shared cluster stays intact.
/// </summary>
public class SupervisorFixture : ClusteringFixture;
