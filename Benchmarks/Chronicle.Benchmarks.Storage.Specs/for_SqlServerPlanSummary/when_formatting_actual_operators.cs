// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_SqlServerPlanSummary;

public class when_formatting_actual_operators : Specification
{
    string _report = default!;

    void Because() => _report = SqlServerPlanSummary.Format("""
        <ShowPlanXML xmlns="http://schemas.microsoft.com/sqlserver/2004/07/showplan">
          <BatchSequence><Batch><Statements><StmtSimple><QueryPlan>
            <QueryTimeStats CpuTime="12" ElapsedTime="18" />
            <RelOp NodeId="0" PhysicalOp="Nested Loops" EstimateRows="100">
              <RunTimeInformation><RunTimeCountersPerThread Thread="0" ActualRows="80" ActualExecutions="1" /></RunTimeInformation>
              <NestedLoops>
                <RelOp NodeId="1" PhysicalOp="Index Seek" EstimateRows="50">
                  <RunTimeInformation>
                    <RunTimeCountersPerThread Thread="1" ActualRows="30" ActualExecutions="1" />
                    <RunTimeCountersPerThread Thread="2" ActualRows="50" ActualExecutions="1" />
                  </RunTimeInformation>
                  <IndexScan><Object Table="[events]" Index="[IX_sequence]" /></IndexScan>
                </RelOp>
                <RelOp NodeId="2" PhysicalOp="Clustered Index Seek" EstimateRows="1">
                  <RunTimeInformation><RunTimeCountersPerThread Thread="0" ActualRows="80" ActualExecutions="80" /></RunTimeInformation>
                  <IndexScan Lookup="1"><Object Table="[events]" Index="[PK_events]" /></IndexScan>
                </RelOp>
                <RelOp NodeId="3" PhysicalOp="Index Scan" EstimateRows="99"><IndexScan><Object Table="[events]" Index="[IX_type]" /></IndexScan></RelOp>
              </NestedLoops>
            </RelOp>
          </QueryPlan></StmtSimple></Statements></Batch></BatchSequence>
        </ShowPlanXML>
        """);

    [Fact] void should_not_count_nested_operator_rows_as_parent_rows() => Assert.Contains("Node 0: Nested Loops; estimated rows=100; actual rows=80; executions=1", _report);
    [Fact] void should_sum_parallel_threads() => Assert.Contains("Node 1: Index Seek; estimated rows=50; actual rows=80; executions=2; [events].[IX_sequence]", _report);
    [Fact] void should_identify_key_lookups() => Assert.Contains("Clustered Index Seek (Key Lookup)", _report);
    [Fact] void should_keep_scans_and_missing_runtime_counters_visible() => Assert.Contains("Node 3: Index Scan; estimated rows=99; actual rows=not reported", _report);
    [Fact] void should_include_server_timings() => Assert.Contains("Query CPU=12 ms; elapsed=18 ms", _report);
}
