import json
import glob

files = sorted(glob.glob('Benchmarks/results/*.json'))
benchmarks = []
host_info = {}
for f in files:
    with open(f) as fp:
        data = json.load(fp)
        benchmarks.extend(data.get('Benchmarks', []))
        host_info = data.get('HostEnvironmentInfo', host_info)

combined = {
    'Title': 'Chronicle Benchmarks',
    'HostEnvironmentInfo': host_info,
    'Benchmarks': benchmarks
}

with open('Benchmarks/results/combined.json', 'w') as fp:
    json.dump(combined, fp)
