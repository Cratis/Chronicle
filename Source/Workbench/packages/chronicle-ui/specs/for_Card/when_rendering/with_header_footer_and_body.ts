// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { Card } from '../../../src';

describe('when rendering a card with header, footer and body', () => {
    let markup: string;

    beforeAll(() => {
        markup = renderToStaticMarkup(createElement(Card, { header: 'the-header', footer: 'the-footer', className: 'extra' }, 'the-body'));
    });

    it('should render the header in the header slot', () => {
        markup.should.match(/workbench-card__header[^>]*>the-header</);
    });

    it('should render the body in the body slot', () => {
        markup.should.match(/workbench-card__body[^>]*>the-body</);
    });

    it('should render the footer in the footer slot', () => {
        markup.should.match(/workbench-card__footer[^>]*>the-footer</);
    });

    it('should apply the caller class next to the card class', () => {
        markup.should.match(/class="[^"]*workbench-card extra[^"]*"/);
    });
});
