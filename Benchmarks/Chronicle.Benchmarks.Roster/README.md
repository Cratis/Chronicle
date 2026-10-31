# Administrator roster read benchmark

Measures `IsActiveAdministrator` for 5, 20 and 100 signed slots, using the same substituted storage and serialized contents for both implementations. Setup warms both rosters before measurement. The baseline project compiles the original roster and its logging helpers verbatim from commit `a5e06dbf0e` into a separately aliased assembly; the current implementation comes from Core. This requires that commit in local Git history.

BenchmarkDotNet runs both methods together in-process, with three warmup and ten measured iterations, 256 invocations per iteration, and memory, mean, median, min/max and error columns. Storage and serializer substitutes record calls; iteration cleanup clears those records outside measured work. This isolates replay cost from database latency; it is not an end-to-end storage benchmark.

Run the Release build and benchmark through pi-phase with low priority. Results belong under `.ai-work/roster-provenance/` rather than this directory.
