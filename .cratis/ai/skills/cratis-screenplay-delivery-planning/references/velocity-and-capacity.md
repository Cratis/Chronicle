<!-- cratis-ai-managed: skills/cratis-screenplay-delivery-planning/references/velocity-and-capacity.md -->
# Velocity and capacity

Read when producing or updating an estimate, or when running a retrospective.

## Measure throughput from history
Throughput is the number of weighted slices a team completes per period, measured from the team's own finished slices.

1. List the completed slices of the last few periods (weeks or iterations). A slice is completed when it is `done` in the status vocabulary: built, specifications passing, conforming to the model.
2. Take dates from the issue or pull request history (opened and closed, or claimed and merged), not from memory.
3. Record per period: slices completed by class, effort spent (person-days or agent-sessions), and anything unusual (leave, outages, a model rework).
4. Compute the median and the lowest and highest period. Report all three.

With fewer than about three completed periods or a handful of slices, the number is a first estimate without history: say so, keep the range wide, and do not extrapolate a trend.

## Estimate as a range
```
remaining weighted slices  = sum over classes (count x team weight)
duration low / expected / high = remaining / (high, median, low throughput)
```
- Report the range and its basis in one line: "N weighted slices; throughput X to Y per week from the last K weeks (slices ...)". Include the separate lines for work that is not slice work.
- Do not state accuracy percentages or comparisons with other methods; they cannot be derived from your data. Calibrate by comparing your past ranges with what happened (below).

## Capacity
Throughput scales with people only along the dependency table: the useful parallelism of a wave is the number of independent slices in it, capped by the number of people or agents. A slice has one owner, so its work cannot be split across workers.
```
lower bound for a wave  = (weighted slices in the wave / workers in use) / throughput per worker
elapsed time for a wave = (largest total weight assigned to one worker) / throughput per worker
total elapsed time      = sum over waves (waves are sequential)
```
- Assign the wave's slices to workers first (heaviest first, each to the least-loaded worker), then take the most-loaded worker. The aggregate formula is only a lower bound; the wave can never finish before its heaviest single slice.
- Example: three independent slices weighted 1, 1 and 3, three workers at one weighted unit per week. The lower bound says 5 / 3 = 1.7 weeks; the assignment gives loads 3, 1 and 1, so the wave takes 3 weeks. Split a heavy slice in the model (slice design) if it dominates a wave, never by sharing it between workers.
- Throughput per worker comes from history with that many workers; adding a worker adds coordination cost, so measure after the change rather than assuming it multiplies.
- Shared contracts (one event consumed by several slices) need one owner; count that integration as work.
- Capacity levers other than people: faster verification (spec run time, CI queue), fewer hand-written slices (a model change that Stage admits), better adapters. Estimate their effect from a before and after measurement, not a promise.

## Retrospective metrics
After each completed group or iteration, record:

| Metric | How | What it shows |
|---|---|---|
| Throughput | weighted slices done per period | the rate used for the next range |
| Slice cycle time by class | claimed to done, per class | which classes carry the cost; recalibrates weights |
| Estimate fit | planned range versus actual, per group | whether ranges were too narrow or too wide |
| Parallel share | slices worked in parallel / slices in waves that allowed it | whether parallelism was used |
| Blocked time | time in `blocked` and the named cause | dependency, decision or external waits |
| Rework | slices reopened after done, or recounted after a model change | model stability |

Use the results to adjust weights, widen or narrow ranges, and decide what to unblock first. Write them in `.ai-work/<task>/delivery-plan.md`; a pattern that must outlive the session becomes an issue.
