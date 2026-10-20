// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.SqlServer;

public class when_a_node_is_killed() : SqlClusterSpecification(false, ClusterChange.Kill);
