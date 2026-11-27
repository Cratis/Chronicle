// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { Card } from '../../../src';

describe('when rendering a card with only a body', () => {
    let markup: string;

    beforeAll(() => {
        markup = renderToStaticMarkup(createElement(Card, undefined, 'the-body'));
    });

    it('should not render a header slot', () => {
        markup.should.not.contain('workbench-card__header');
    });

    it('should not render a footer slot', () => {
        markup.should.not.contain('workbench-card__footer');
    });
});
