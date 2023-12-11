import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
// import Backend from 'i18next-fs-backend';
import * as  translationEN from '../Locales/en/translation.json';
import * as  translationNO from '../Locales/no/translation.json';


const resources = {
    en: {
        translation: translationEN,
    },
    no: {
        translation: translationNO,
    }
};

i18n
    .use(initReactI18next) // passes i18n down to react-i18next
    // .use(Backend)
    .init({
        resources,
        // backend: {
        //     loadPath: '/Locales/{{lng}}/translation.json',
        //     addPath: '/Locales/{{lng}}/translation.missing.json',
        // },

        fallbackLng: 'en',
        // you can use the i18n.changeLanguage function to change the language manually: https://www.i18next.com/overview/api#changelanguage
        // if you're using a language detector, do not define the lng option
        debug: false,
        react: {
            useSuspense: false,
            transSupportBasicHtmlNodes: true,
            transKeepBasicHtmlNodesFor: ['u', 'a', 'ul', 'li', 'p', 'br']
        },
        interpolation: {
            escapeValue: false // react already safes from xss
        }
    });

export default i18n;
