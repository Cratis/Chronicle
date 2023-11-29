// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Page } from 'Components/Common/Page';
import { useTranslation } from "react-i18next";

export const Recommendations = () => {
    const { t } = useTranslation();
    return <Page title={t('EventStore.Tenant.Recommendations.Title')}/>;
};
