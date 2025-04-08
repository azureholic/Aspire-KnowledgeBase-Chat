import { Configuration, LogLevel } from "@azure/msal-browser";

export const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_PUBLIC_APP_ID as string,
    authority: import.meta.env.VITE_PUBLIC_AUTHORITY_URL as string,
    redirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: "sessionStorage",
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) {
          return;
        }
        switch (level) {
          case LogLevel.Error:
            console.error(message);
            break;
          case LogLevel.Info:
            console.info(message);
            break;
          case LogLevel.Verbose:
            console.debug(message);
            break;
          case LogLevel.Warning:
            console.warn(message);
            break;
        }
      },
      logLevel: LogLevel.Verbose
    }
  }
};

// Scopes for token acquisition - used for login
export const loginRequest = {
  scopes: ["User.Read", "openid", "profile", "email"]
};

// Add here the scopes to request when obtaining an access token for the API
export const apiRequest = {
  scopes: [(import.meta.env.VITE_BACKEND_SCOPE as string)]
};