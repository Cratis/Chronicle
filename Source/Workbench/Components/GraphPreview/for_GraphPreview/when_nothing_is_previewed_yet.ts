// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { PrimeReactProvider } from '@primereact/core';
import strings from 'Strings';
import { GraphPreview } from '../GraphPreview';
import { defaultGraphViewSettings } from '../graphViews';

describe('when nothing is previewed yet', () => {
    let html: string;

    beforeEach(() => {
        html = renderToStaticMarkup(createElement(PrimeReactProvider, null, createElement(GraphPreview, {
            preview: null,
            isPerforming: false,
            settings: defaultGraphViewSettings,
            onReadView: () => { }
        })));
    });

    it('should say how to preview', () => html.should.include('Press Preview'));
    it('should say the preview runs in memory', () => html.should.include(strings.eventStore.general.projections.graphPreview.notPreviewed));
    it('should mark the empty state', () => html.should.include('data-testid="graph-preview-empty"'));
    it('should not show the tabs', () => html.should.not.include('Nodes ('));
});
