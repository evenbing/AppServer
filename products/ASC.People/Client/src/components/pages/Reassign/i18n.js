import i18n from "i18next";
import Backend from "i18next-xhr-backend";
import config from "../../../../package.json";

const newInstance = i18n.createInstance();

if (process.env.NODE_ENV === "production") {
  newInstance
    .use(Backend)
    .init({
      lng: 'en',
      fallbackLng: "en",
      debug: true,

      interpolation: {
        escapeValue: false // not needed for react as it escapes by default
      },

      react: {
        useSuspense: true
      },
      backend: {
        loadPath: `${config.homepage}/locales/Reassign/{{lng}}/{{ns}}.json`
      }
    });
} else if (process.env.NODE_ENV === "development") {

  const resources = {
    en: {
      translation: require("./locales/en/translation.json")
    }
  };

  newInstance.init({
    resources: resources,
    lng: 'en',
    fallbackLng: "en",
    debug: true,

    interpolation: {
      escapeValue: false // not needed for react as it escapes by default
    },

    react: {
      useSuspense: true
    }
  });
}

export default newInstance;