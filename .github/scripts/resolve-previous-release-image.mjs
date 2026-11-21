// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { spawnSync } from 'node:child_process';
import { setTimeout } from 'node:timers/promises';
import { pathToFileURL } from 'node:url';

function lookupError(imageTag) {
    const response = spawnSync('curl', [
        '--fail', '--silent', '--show-error', '--location', '--max-time', '60',
        `https://hub.docker.com/v2/repositories/cratis/chronicle/tags/${imageTag}`
    ], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'inherit'] });
    if (response.status !== 0) return `Previous release image cratis/chronicle:${imageTag} could not be resolved on Docker Hub.`;
    try {
        const metadata = JSON.parse(response.stdout);
        if (metadata.name === imageTag && Array.isArray(metadata.images) && metadata.images.length > 0) return null;
    } catch {
        // Invalid metadata is not evidence that an image has been published.
    }
    return `Docker Hub returned no image for cratis/chronicle:${imageTag}.`;
}

export async function waitForPreviousReleaseImage(imageTag, delay = () => setTimeout(30000)) {
    let error;
    for (let attempt = 1; attempt <= 10; attempt++) {
        error = lookupError(imageTag);
        if (error === null) return true;
        console.log(`::warning::${error} (attempt ${attempt}/10)`);
        if (attempt < 10) await delay();
    }
    console.error(`::error::${error}`);
    return false;
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
    process.exitCode = await waitForPreviousReleaseImage(process.argv[2]) ? 0 : 1;
}
