// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export const switchThemeCss = (newTheme: string, currentMode: string, linkElementId: string) => {
    let _linkElement$parentNo
    const linkElement = document.getElementById(linkElementId) as HTMLLinkElement;
    if (!linkElement) return;
    const cloneLinkElement = linkElement.cloneNode(true) as HTMLLinkElement;
    const oldThemeUrl = linkElement.getAttribute('href');
    if (!oldThemeUrl) return;
    const newThemeUrl = oldThemeUrl.replace(currentMode, newTheme);
    cloneLinkElement.setAttribute('id', linkElementId + '-clone');
    cloneLinkElement.setAttribute('href', newThemeUrl);
    cloneLinkElement.addEventListener('load', () => {
        cloneLinkElement.setAttribute('id', linkElementId);
        document.getElementById(linkElementId + '-old')?.remove();
    });
    linkElement.setAttribute('id', linkElementId + '-old');
    (_linkElement$parentNo = linkElement.parentNode) === null || _linkElement$parentNo === void 0 || _linkElement$parentNo.insertBefore(cloneLinkElement, linkElement.nextSibling);
}
