// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { Button } from 'Components/Button';
import { DialogButtons, DialogResult, useConfirmationDialog } from '@cratis/arc.react/dialogs';
import strings from 'Strings';
import { Page } from 'Components/Common/Page';
import { AreDevelopmentToolsAvailable, ResetKernelState } from 'Features/DevelopmentTools';
import './DevelopmentTools.css';

/**
 * Tools that only exist while developing against a local Chronicle.
 *
 * The page is only reachable when the server reports that its development tools were compiled in;
 * a production server has no reset endpoint to call at all.
 * @returns The rendered page.
 */
export const DevelopmentTools = () => {
    const [availability] = AreDevelopmentToolsAvailable.use();
    const [showConfirmation] = useConfirmationDialog();
    const [isResetting, setIsResetting] = useState(false);
    const [wasReset, setWasReset] = useState(false);

    const developmentToolsStrings = strings.eventStore.system.developmentTools;

    const reset = async () => {
        const confirmed = await showConfirmation(
            developmentToolsStrings.reset.confirmTitle,
            developmentToolsStrings.reset.confirmMessage,
            DialogButtons.YesNo);
        if (confirmed !== DialogResult.Yes) return;

        setIsResetting(true);
        setWasReset(false);
        try {
            const result = await new ResetKernelState().execute();
            setWasReset(result.isSuccess);
        } finally {
            setIsResetting(false);
        }
    };

    // The route exists in every build so it cannot be shadowed by the namespace routes, but the
    // tools themselves only exist when the server was built with them compiled in. The check is
    // against an explicit false: the query starts out with no answer at all, which is not a no.
    if (availability.data.isAvailable === false) {
        return (
            <Page title={developmentToolsStrings.title}>
                <p className='development-tools__description'>{developmentToolsStrings.unavailable}</p>
            </Page>
        );
    }

    return (
        <Page title={developmentToolsStrings.title}>
            <section className='development-tools__action'>
                <h2 className='development-tools__heading'>{developmentToolsStrings.reset.title}</h2>
                <p className='development-tools__description'>{developmentToolsStrings.reset.description}</p>
                <Button
                    severity='danger'
                    icon='pi pi-trash'
                    label={developmentToolsStrings.reset.action}
                    loading={isResetting}
                    onClick={reset} />
                {wasReset && <p className='development-tools__done'>{developmentToolsStrings.reset.done}</p>}
            </section>
        </Page>
    );
};
