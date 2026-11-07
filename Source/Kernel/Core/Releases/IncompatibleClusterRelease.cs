// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Releases;

/// <summary>
/// Exception that gets thrown when a silo starts in a cluster with a release it cannot exchange grain calls with.
/// </summary>
/// <param name="local">The release of this silo.</param>
/// <param name="incompatible">The releases in the cluster that are incompatible with it.</param>
public class IncompatibleClusterRelease(SemanticVersion local, IReadOnlyList<SemanticVersion> incompatible)
    : Exception($"Chronicle {local} cannot share a cluster with {string.Join(", ", incompatible)}: the serializers were renamed in {ClusterReleaseBoundary.SerializerRename}. Stop every silo and start the new release, for example with the Recreate strategy in Kubernetes. See https://cratis.io/chronicle/hosting/rolling-deploy/");
