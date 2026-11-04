// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('../../', import.meta.url));
const read = path => readFileSync(new URL(`../../${path}`, import.meta.url), 'utf8');
const localSlim = 'cratis/chronicle:local-development-slim';
const suites = [
    ['Integration/Client/Client.csproj', 'Integration/Client/ChronicleConfigurableFixture.cs'],
    ['Integration/SqlClustering/SqlClustering.csproj', 'Integration/SqlClustering/SqlCluster.cs']
];

function imageProperty(project, override, disabled) {
    const env = { ...process.env };
    delete env.CRATIS_CHRONICLE_LOCAL_IMAGE;
    if (override !== undefined) env.CRATIS_CHRONICLE_LOCAL_IMAGE = override;
    return execFileSync('dotnet', ['msbuild', project, '-nologo', '-getProperty:LocalImageName',
        `-p:DisableDockerBuild=${disabled}`], { cwd: root, env, encoding: 'utf8', timeout: 20000 }).trim();
}

for (const [project, fixture] of suites) {
    test(`${project} defaults to a local slim image and propagates explicit overrides`, () => {
        for (const disabled of [false, true]) {
            assert.equal(imageProperty(project, undefined, disabled), localSlim);
            assert.equal(imageProperty(project, '', disabled), localSlim);
            assert.equal(imageProperty(project, 'ghcr.io/cratis/chronicle:test-override', disabled),
                'ghcr.io/cratis/chronicle:test-override');
        }
        assert.match(read(project), /<Exec Command="docker build -t \$\(LocalImageName\) -f Docker\/LocalSlim\/Dockerfile/);
        assert.match(read(project), /<Target Name="BuildDocker" AfterTargets="Build" Condition="'\$\(DisableDockerBuild\)' != 'true'">/);
    });

    test(`${fixture} uses the same fallback and runtime override as its producer`, () => {
        const fallback = read(fixture).match(/Environment\.GetEnvironmentVariable\("CRATIS_CHRONICLE_LOCAL_IMAGE"\) \?\? "([^"]+)"/);
        assert.ok(fallback, 'runtime image must honor CRATIS_CHRONICLE_LOCAL_IMAGE');
        assert.equal(fallback[1], localSlim);
    });
}

test('dockerize uses explicit unpublished production and development tags', () => {
    const commands = read('dockerize.sh').split('\n').filter(line => /^\s*docker build\b/.test(line));
    assert.equal(commands.length, 2);
    assert.match(commands[0], /^docker build -t cratis\/chronicle:local -f \.\/Docker\/Production\/Dockerfile \.$/);
    assert.match(commands[1], /^docker build -t cratis\/chronicle:local-development -f \.\/Docker\/Development\/Dockerfile \.$/);
    assert.match(read('dockerize.sh'), /#docker buildx build -t cratis\/chronicle:local-development -f/);
});
