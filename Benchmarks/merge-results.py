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
