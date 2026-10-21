// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.SqlServer;

public class when_restarting_nodes_one_by_one_under_load() : SqlClusterSpecification(false, ClusterChange.Restart);
