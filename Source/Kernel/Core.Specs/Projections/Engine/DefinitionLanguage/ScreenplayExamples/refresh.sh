#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
# Refresh the projection examples from the centrally pinned Screenplay tag. Requires gh and python3.
set -euo pipefail
python3 - "$(cd "$(dirname "$0")" && pwd)" <<'PY'
import base64
import json
from pathlib import Path
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

output = Path(sys.argv[1])
root = next(parent for parent in output.parents if (parent / 'Directory.Packages.props').is_file())
version = next(item.attrib['Version'] for item in ET.parse(root / 'Directory.Packages.props').iter()
               if item.tag.rsplit('}', 1)[-1] == 'PackageVersion'
               if item.attrib.get('Include') == 'Cratis.Screenplay')
tag = 'v' + version


def api(path):
    return json.loads(subprocess.check_output(['gh', 'api', 'repos/Cratis/Screenplay/' + path], text=True))


commit = api('commits/' + tag)['sha']
tree = api('git/trees/' + commit + '?recursive=1')
if tree.get('truncated'):
    raise SystemExit('GitHub returned a truncated tree')
paths = sorted(item['path'] for item in tree['tree']
               if item['path'].startswith('Documentation/screenplay/projections/')
               and item['path'].endswith(('.md', '.mdx')))
if not paths:
    raise SystemExit('No projection documentation found at ' + tag)

# Port of for_Documentation/given/DocumentationExamples.cs at the pinned tag.
projection_level = {'from', 'every', 'children', 'nested', 'join', 'remove', 'automap', 'no'}
mapping_directives = ('key ', 'add ', 'subtract ', 'parent ')
placeholder = re.compile(r'<[A-Za-z][\w .]*>|\{[A-Za-z][\w .]*\}')


def dedent(body):
    indent = min((len(line) - len(line.lstrip()) for line in body if line.strip()), default=0)
    return '\n'.join(line[indent:] if line.strip() else '' for line in body)


def wrap(header, body):
    padding = ' ' * (len(header.split('\n')) * 2)
    return header + '\n' + ''.join((padding + line if line else '') + '\n' for line in dedent(body).split('\n'))


def classify(body):
    text = '\n'.join(body)
    if not text.strip() or placeholder.search(text) or any(line.strip() in ('...', '// ...') for line in body):
        return None, 'Syntax template or elided example (upstream DocumentationExamples.Classify)'
    first = next((line.strip() for line in body if line.strip() and not line.lstrip().startswith(('//', '#'))), '')
    keyword = next(iter(first.split()), '')
    has_mapping = any('=' in line for line in body)
    has_directive = any(line.lstrip().startswith(mapping_directives) for line in body)
    if not has_mapping and not has_directive and keyword not in projection_level:
        return None, 'Expression vocabulary, not a declaration (upstream DocumentationExamples.Classify)'
    if keyword == 'projection':
        return dedent(body), 'projection'
    if keyword in projection_level:
        return wrap('projection Doc => DocReadModel', body), 'projection body'
    if keyword == 'parent':
        return wrap('projection Doc => DocReadModel\n  children docs identified by docId\n    from DocEvent', body), 'child mapping'
    return wrap('projection Doc => DocReadModel\n  from DocEvent', body), 'mapping block'


examples = []
skipped = []
files = {}
for path in paths:
    doc = api('contents/' + path + '?ref=' + commit)
    lines = base64.b64decode(doc['content']).decode('utf-8').splitlines()
    index = 0
    while index < len(lines):
        fence = re.fullmatch(r'(`{3,4})pdl\s*', lines[index].strip())
        if not fence:
            index += 1
            continue
        line = index + 1
        index += 1
        body = []
        while index < len(lines) and lines[index].strip() != fence.group(1):
            body.append(lines[index])
            index += 1
        if index == len(lines):
            raise SystemExit(f'Unclosed pdl fence at {path}:{line}')
        source, kind = classify(body)
        reference = {'path': path, 'line': line}
        if source is None:
            skipped.append({**reference, 'reason': kind})
        else:
            filename = f'{Path(path).stem}-{line:04d}.pdl'
            files[filename] = source.rstrip('\n') + '\n'
            examples.append({**reference, 'file': filename, 'kind': kind})
        index += 1

if not examples:
    raise SystemExit('No compilable projection examples found')
# Fetch everything before replacing the snapshot; a failed fetch never publishes a partial corpus.
previous = output / 'provenance.json'
if previous.is_file():
    for example in json.loads(previous.read_text())['examples']:
        stale = example['file']
        if stale not in files:
            (output / stale).unlink()
for filename, source in files.items():
    (output / filename).write_text(source, encoding='utf-8', newline='\n')
provenance = {'version': version, 'tag': tag, 'commit': commit, 'examples': examples, 'skipped': skipped}
previous.write_text(json.dumps(provenance, indent=2, ensure_ascii=False) + '\n', encoding='utf-8', newline='\n')
print(f'{tag} ({commit}): {len(examples)} examples, {len(skipped)} non-example fences')
PY
