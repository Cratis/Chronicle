// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventStoreCard } from 'Components/Common/EventStoreCard';
import { EventStoreKeyFigures } from 'Components/Common/EventStoreKeyFigures';
import { HomeViewModel } from './HomeViewModel';
import { withViewModel } from '@cratis/arc.react.mvvm';
import { useRelativePath } from '../Utils/useRelativePath';
import css from './Home.module.css';
import { MdAdd } from 'react-icons/md';
import strings from 'Strings';
import { useDialog } from '@cratis/arc.react/dialogs';
import { AddEventStoreDialog } from './AddEventStoreDialog';
import chronicleLogo from './Security/chronicle.svg';
import { version } from '../version';

export const Home = withViewModel(HomeViewModel, ({ viewModel }) => {
    const basePath = useRelativePath('event-store');
    const [AddEventStoreDialogWrapper, showAddEventStoreDialog] = useDialog(AddEventStoreDialog);

    return (
        <div className={css.home}>
            <header className={css.header}>
                <img src={chronicleLogo} alt='Chronicle' className={css.logo} />
                <div className={css.heading}>
                    <h1 className={css.title}>{strings.home.selectHeader}</h1>
                    <span className={css.subtitle}>{strings.home.subtitle}</span>
                </div>
                <span className={css.version}>v{version.version}</span>
            </header>

            <div className={css.grid}>
                {viewModel.eventStores.map((eventStore) => (
                    <EventStoreCard
                        key={eventStore}
                        title={eventStore}
                        path={`${basePath}/${eventStore}`}
                        footer={<EventStoreKeyFigures eventStore={eventStore} />}
                    />
                ))}

                <button type='button' className={css.addCard} onClick={() => showAddEventStoreDialog()}>
                    <MdAdd size={32} aria-hidden='true' />
                    <span>{strings.home.addEventStore}</span>
                </button>
            </div>
            <AddEventStoreDialogWrapper />
        </div>
    );
});
