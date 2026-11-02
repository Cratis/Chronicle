// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { ProjectionAction } from '../ProjectionAction';
import strings from 'Strings';

const scenarios = [
    { label: strings.eventStore.general.projections.actions.save, reason: strings.eventStore.general.projections.saveDisabledReasons.readModelOwnedByCode },
    { label: strings.eventStore.general.projections.actions.save, reason: strings.eventStore.general.projections.saveDisabledReasons.validationErrors },
    { label: strings.eventStore.general.projections.actions.preview, reason: strings.eventStore.general.projections.previewDisabledReasons.validationErrors },
];

scenarios.forEach(({ label, reason }) => {
    describe(`when ${label} is disabled because ${reason}`, () => {
        let html: string;

        beforeEach(() => {
            html = renderToStaticMarkup(createElement(ProjectionAction, {
                label,
                disabledReason: reason,
            }));
        });

        it('should offer a keyboard focus target', () => html.should.include('tabindex="0"'));
        it('should announce that the action is disabled', () => html.should.include('aria-disabled="true"'));
        it('should associate the reason with the focus target', () => {
            const descriptionId = html.match(/aria-describedby="([^"]+)"/)?.[1];
            html.should.include(`id="${descriptionId}"`);
        });
        it('should include the current reason', () => html.should.include(reason));
        it('should keep the inner button natively disabled', () => html.should.include('disabled=""'));
        it('should let pointer events reach the tooltip trigger', () => html.should.include('pointer-events:none'));
    });
});
