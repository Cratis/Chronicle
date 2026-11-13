// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const ordinal = (a, b) => a < b ? -1 : a > b ? 1 : 0;
export const slowest = shards => Math.max(...shards.map(shard => shard.seconds));
function assign(jobs, count) {
    const shards = Array.from({ length: count }, (_, index) => ({ shard: index + 1, projects: [], seconds: 0 }));
    for (const job of jobs) {
        const shard = shards.reduce((best, candidate) => candidate.seconds < best.seconds ? candidate : best);
        shard.projects.push(job.project);
        shard.seconds += job.seconds;
    }
    return shards;
}
export function balanceJobs(jobs, count = 3) {
    const sorted = [...jobs].sort((a, b) => b.seconds - a.seconds || ordinal(a.project, b.project));
    const greedy = assign(sorted, count);
    // Also try reserving capacity for Core before placing large indivisible projects.
    // Keep normal LPT when spreading Core would increase the critical path.
    const seeded = assign([...sorted.filter(job => job.core), ...sorted.filter(job => !job.core)], count);
    return slowest(seeded) < slowest(greedy) ? seeded : greedy;
}
export function balancePrefixes(units, count) {
    const jobs = units.map(unit => ({ project: unit.prefix, seconds: unit.weight }));
    const weights = new Map(units.map(unit => [unit.prefix, unit]));
    // Namespace assignment remains ordinary deterministic LPT, without Core seeding.
    return balanceJobs(jobs, count).map(shard => ({
        part: shard.shard, weight: shard.seconds, units: shard.projects.map(prefix => weights.get(prefix))
    }));
}
