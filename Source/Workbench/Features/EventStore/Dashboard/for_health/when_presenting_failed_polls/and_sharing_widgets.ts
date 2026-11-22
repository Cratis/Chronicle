// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { a_dashboard } from './given/a_dashboard';
import { HealthTiles } from '../../HealthTiles';
import { NamespacesWidget } from '../../NamespacesWidget';
import { summarizeObservers } from '../../health';
import strings from 'Strings';

const texts = strings.eventStore.dashboard;
const health = { observers: summarizeObservers([]), failedPartitions: 0, recommendations: 0, runningJobs: 0 };

describe('when sharing dashboard widgets without failure flags', () => {
    let context: a_dashboard;
    let healthyTiles: string;
    let pendingTiles: string;
    let namespaces: string;

    beforeEach(() => {
        context = new a_dashboard();
        healthyTiles = renderToStaticMarkup(createElement(HealthTiles, { health }));
        pendingTiles = renderToStaticMarkup(createElement(HealthTiles, { health: undefined }));
        namespaces = renderToStaticMarkup(createElement(NamespacesWidget, {
            namespaces: ['first'], health: { first: health }, eventCounts: {}, basePath: '/the-store'
        }));
    });
    afterEach(() => context.dispose());

    it('should preserve healthy tile captions', () => healthyTiles.should.include(texts.allPartitionsHealthy));
    it('should preserve healthy tile checkmarks', () => healthyTiles.should.include('✓'));
    it('should preserve pending tile placeholders', () => pendingTiles.should.include('dashboard-tile__value">–'));
    it('should preserve zero counts for missing entries in a successful count map', () => namespaces.should.include('<td><span class="dashboard-table__muted">0</span></td>'));
    it('should preserve namespace drill-down links', () => namespaces.should.include('href="/the-store/first/dashboard"'));
});
