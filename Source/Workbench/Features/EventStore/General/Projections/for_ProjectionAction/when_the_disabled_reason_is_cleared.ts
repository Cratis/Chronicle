// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { ProjectionAction } from '../ProjectionAction';
import strings from 'Strings';

describe('when the disabled reason is cleared', () => {
    let html: string;

    beforeEach(() => {
        html = renderToStaticMarkup(createElement(ProjectionAction, {
            label: strings.eventStore.general.projections.actions.save,
            disabledReason: undefined,
        }));
    });

    it('should render the enabled action', () => html.should.include('<button'));
    it('should not disable the button', () => html.should.not.include('disabled='));
    it('should not leave a disabled explanation', () => html.should.not.include('aria-describedby='));
    it('should not leave an extra keyboard focus target', () => html.should.not.include('tabindex="0"'));
});
