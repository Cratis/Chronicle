// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { HeaderSummary } from '../HeaderSummary';

describe('when header metadata is unavailable', () => {
    let html: string;

    beforeEach(() => {
        html = renderToStaticMarkup(createElement(HeaderSummary, {
            headers: { 'x-key': 'synthetic-sensitive-value', 'x-empty': '' },
        }));
    });

    it('should still show names from an older response', () => html.should.include('x-key:'));
    it('should not infer presence from legacy header values', () => html.should.not.include('Value set'));
    it('should not infer absence from redacted header values', () => html.should.not.include('Not set'));
    it('should explain that metadata is unavailable', () => html.should.include('Value unavailable'));
    it('should never render a legacy header value', () => html.should.not.include('synthetic-sensitive-value'));
});
