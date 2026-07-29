import json
import glob

files = sorted(glob.glob('Benchmarks/results/*.json'))
benchmarks = []
host_info = {}
for f in files:
    with open(f) as fp:
        data = json.load(fp)
        for benchmark in data.get('Benchmarks', []):
            statistics = benchmark.get('Statistics') or {}
            if statistics.get('Mean') is None:
                print(f"Skipping benchmark without mean statistics: {benchmark.get('FullName', '[unknown]')}")
                continue
            benchmarks.append(benchmark)
        host_info = data.get('HostEnvironmentInfo', host_info)

combined = {
    'Title': 'Chronicle Benchmarks',
    'HostEnvironmentInfo': host_info,
    'Benchmarks': benchmarks
}

with open('Benchmarks/results/combined.json', 'w') as fp:
    json.dump(combined, fp)

# A run that discovers nothing still exports a well-formed file with an empty Benchmarks array. Say so here,
# rather than letting the publishing step fail with the far less obvious "no benchmark result was found".
if not benchmarks:
    raise SystemExit(
        f'No benchmarks were recorded across {len(files)} result file(s). '
        'The run discovered no benchmarks, which usually means the benchmark project failed to build.')
