// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { PrimeReactProvider } from '@primereact/core';
import { GraphPreview } from '../GraphPreview';
import { defaultGraphViewSettings } from '../graphViews';
import { organizationPreview } from './given/previews';

describe('when rendering a preview', () => {
    let html: string;

    beforeEach(() => {
        html = renderToStaticMarkup(createElement(PrimeReactProvider, null, createElement(GraphPreview, {
            preview: organizationPreview(),
            isPerforming: false,
            settings: defaultGraphViewSettings,
            onReadView: () => { }
        })));
    });

    it('should summarize the graph', () => html.should.include('Organization: 3 nodes and 1 edges from 5 events'));
    it('should offer the nodes tab with its count', () => html.should.include('Nodes (3)'));
    it('should offer the edges tab with its count', () => html.should.include('Edges (1)'));
    it('should offer the view tab', () => html.should.include('>View<'));
    it('should show the nodes', () => html.should.include('Ada'));
    it('should not show errors', () => html.should.not.include('role="alert"'));
});
