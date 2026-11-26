// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { HeaderSummary } from '../HeaderSummary';

describe('when showing configured headers', () => {
    let html: string;

    beforeEach(() => {
        html = renderToStaticMarkup(createElement(HeaderSummary, {
            headers: { 'x-key': 'synthetic-sensitive-value', 'x-empty': '' },
            headerValuesSet: { 'x-key': true, 'x-empty': false },
        }));
    });

    it('should show configured header names', () => html.should.include('x-key:'));
    it('should show that a value is set', () => html.should.include('Value set'));
    it('should show that an empty value is not set', () => html.should.include('Not set'));
    it('should never render a header value', () => html.should.not.include('synthetic-sensitive-value'));
});
