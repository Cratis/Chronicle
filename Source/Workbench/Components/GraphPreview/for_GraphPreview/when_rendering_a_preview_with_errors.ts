// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { PrimeReactProvider } from '@primereact/core';
import { GraphPreview } from '../GraphPreview';
import { defaultGraphViewSettings } from '../graphViews';
import { organizationPreview } from './given/previews';

describe('when rendering a preview with errors', () => {
    let html: string;

    beforeEach(() => {
        html = renderToStaticMarkup(createElement(PrimeReactProvider, null, createElement(GraphPreview, {
            preview: organizationPreview({
                eventErrors: [{ sequenceNumber: 8, message: 'The node still has edges' }],
                viewErrors: [{ reason: 'UnknownEdge', view: 'People', member: 'friends', message: 'The graph has no edge FRIEND_OF' }]
            }),
            isPerforming: false,
            settings: defaultGraphViewSettings,
            onReadView: () => { }
        })));
    });

    it('should show the event that could not be applied', () => html.should.include('#8: The node still has edges'));
    it('should announce the event errors', () => html.should.include('data-testid="graph-preview-event-errors"'));
    it('should show the view error with its reason', () => html.should.include('UnknownEdge'));
    it('should show where the view error is', () => html.should.include('(People.friends)'));
});
