// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { withViewModel } from '@cratis/arc.react.mvvm';
import { Button } from 'primereact/button';
import { Dialog } from 'primereact/dialog';
import { AddEventTypeViewModel } from './AddEventTypeViewModel';
import { InputText } from 'primereact/inputtext';
import strings from 'Strings';

export class AddEventTypeRequest {
}

export class AddEventTypeResponse {
    constructor(readonly name: string) {
    }
}

export const AddEventType = withViewModel<AddEventTypeViewModel>(AddEventTypeViewModel, ({ viewModel }) => {
    return (
        <div>
            <Dialog header={strings.eventStore.general.types.dialogs.addEventType.title} visible={true} style={{ width: '20vw' }} modal onHide={() => viewModel.cancel()}>
                <div className="card flex flex-column md:flex-row gap-3">
                    <div className="p-inputgroup flex-1">
                        <span className="p-inputgroup-addon">
                            <i className="pi pi-user"></i>
                        </span>
                        <InputText placeholder={strings.eventStore.general.types.dialogs.addEventType.name} value={viewModel.name} onChange={e => viewModel.name = e.target.value} />
                    </div>
                </div>

                <div className="card flex flex-wrap justify-content-center gap-3 mt-8">
                    <Button label={strings.general.buttons.ok} icon="pi pi-check" onClick={() => viewModel.proceed()} autoFocus />
                    <Button label={strings.general.buttons.cancel} icon="pi pi-times" severity='secondary' onClick={() => viewModel.cancel()} />
                </div>
            </Dialog>
        </div>
    );
});
