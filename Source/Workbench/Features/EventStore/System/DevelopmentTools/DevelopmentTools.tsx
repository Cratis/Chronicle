// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { AllEventStores } from 'Api/EventStores';
import { ResetAll, ResetEventStore } from 'Api/DevelopmentTools';
import { useConfirmationDialog, DialogButtons, DialogResult } from '@cratis/arc.react/dialogs';
import strings from 'Strings';

export const DevelopmentTools = () => {
    const [eventStoresResult] = AllEventStores.use();
    const [resetAll] = ResetAll.use();
    const [resetEventStore] = ResetEventStore.use();
    const [showConfirmation] = useConfirmationDialog();
    const [resetAllDone, setResetAllDone] = useState(false);
    const [resetAllRunning, setResetAllRunning] = useState(false);
    const [resetEventStoreDone, setResetEventStoreDone] = useState<string | null>(null);
    const [resetEventStoreRunning, setResetEventStoreRunning] = useState(false);

    const devToolsStrings = strings.eventStore.system.developmentTools;

    const handleResetAll = async () => {
        const result = await showConfirmation(
            devToolsStrings.resetAll.confirmTitle,
            devToolsStrings.resetAll.confirmMessage,
            DialogButtons.YesNo
        );

        if (result !== DialogResult.Yes) return;

        setResetAllRunning(true);
        setResetAllDone(false);
        try {
            await resetAll.execute();
            setResetAllDone(true);
        } finally {
            setResetAllRunning(false);
        }
    };

    const handleResetEventStore = async (eventStore: string) => {
        const message = devToolsStrings.resetEventStore.confirmMessage.replace('{eventStore}', eventStore);
        const result = await showConfirmation(
            devToolsStrings.resetEventStore.confirmTitle,
            message,
            DialogButtons.YesNo
        );

        if (result !== DialogResult.Yes) return;

        setResetEventStoreRunning(true);
        setResetEventStoreDone(null);
        try {
            resetEventStore.eventStore = eventStore;
            await resetEventStore.execute();
            setResetEventStoreDone(eventStore);
        } finally {
            setResetEventStoreRunning(false);
        }
    };

    const eventStores = eventStoresResult.data ?? [];

    return (
        <div className='p-6 space-y-8 max-w-3xl'>
            <h1 className='text-2xl font-semibold'>{devToolsStrings.title}</h1>

            {/* Reset All section */}
            <section className='border border-red-300 rounded-lg p-6 bg-red-50 space-y-4'>
                <h2 className='text-lg font-semibold text-red-700'>{devToolsStrings.resetAll.title}</h2>
                <p className='text-sm text-gray-700'>{devToolsStrings.resetAll.description}</p>

                {resetAllDone && (
                    <p className='text-sm text-green-700 font-medium'>{devToolsStrings.resetAll.success}</p>
                )}

                <button
                    className='px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded font-medium disabled:opacity-50 disabled:cursor-not-allowed'
                    disabled={resetAllRunning}
                    onClick={handleResetAll}>
                    {resetAllRunning ? '...' : devToolsStrings.resetAll.button}
                </button>
            </section>

            {/* Reset Event Store section */}
            <section className='border border-orange-300 rounded-lg p-6 bg-orange-50 space-y-4'>
                <h2 className='text-lg font-semibold text-orange-700'>{devToolsStrings.resetEventStore.title}</h2>
                <p className='text-sm text-gray-700'>{devToolsStrings.resetEventStore.description}</p>

                {resetEventStoreDone && (
                    <p className='text-sm text-green-700 font-medium'>
                        {devToolsStrings.resetEventStore.success.replace('{eventStore}', resetEventStoreDone)}
                    </p>
                )}

                {eventStores.length === 0 ? (
                    <p className='text-sm text-gray-500'>{devToolsStrings.resetEventStore.empty}</p>
                ) : (
                    <ul className='space-y-2'>
                        {eventStores.map(eventStore => (
                            <li key={eventStore} className='flex items-center justify-between p-3 bg-white border border-orange-200 rounded'>
                                <span className='font-mono text-sm'>{eventStore}</span>
                                <button
                                    className='px-3 py-1 bg-orange-500 hover:bg-orange-600 text-white rounded text-sm font-medium disabled:opacity-50 disabled:cursor-not-allowed'
                                    disabled={resetEventStoreRunning}
                                    onClick={() => handleResetEventStore(eventStore)}>
                                    {resetEventStoreRunning ? '...' : devToolsStrings.resetEventStore.button}
                                </button>
                            </li>
                        ))}
                    </ul>
                )}
            </section>
        </div>
    );
};
