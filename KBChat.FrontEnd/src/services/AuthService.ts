import { PublicClientApplication, InteractionRequiredAuthError, AccountInfo } from "@azure/msal-browser";
import { msalConfig, apiRequest, loginRequest } from "../authConfig";

class AuthService {
  private msalInstance: PublicClientApplication;

  constructor() {
    this.msalInstance = new PublicClientApplication(msalConfig);
  }

  async getToken(): Promise<string | null> {
    const activeAccount = this.msalInstance.getActiveAccount();
    
    if (!activeAccount) {
      // No active account, the user must sign-in first
      const accounts = this.msalInstance.getAllAccounts();
      
      if (accounts.length === 0) {
        // No accounts found, user needs to login interactively
        return null;
      } else {
        // Set the first account as active
        this.msalInstance.setActiveAccount(accounts[0]);
      }
    }
    
    try {
      const silentRequest = {
        ...apiRequest,
        account: this.msalInstance.getActiveAccount()!
      };
      
      // Try to acquire token silently
      const response = await this.msalInstance.acquireTokenSilent(silentRequest);
      return response.accessToken;
    } catch (error) {
      if (error instanceof InteractionRequiredAuthError) {
        // Silent token acquisition failed, user interaction is required
        return null;
      }
      console.error("Error acquiring token:", error);
      return null;
    }
  }

  async login(): Promise<void> {
    try {
      const response = await this.msalInstance.loginPopup(loginRequest);
      // Save the ID token in session storage for SSO
      if (response) {
        sessionStorage.setItem('idToken', response.idToken);
        this.msalInstance.setActiveAccount(response.account);
      }
    } catch (error) {
      console.error("Login failed", error);
    }
  }

  async logout(): Promise<void> {
    try {
      // Clear any saved tokens
      sessionStorage.removeItem('idToken');
      
      // Logout from MSAL
      await this.msalInstance.logoutPopup({
        mainWindowRedirectUri: window.location.origin,
      });
    } catch (error) {
      console.error("Logout failed", error);
    }
  }

  getIdToken(): string | null {
    return sessionStorage.getItem('idToken');
  }

  getAccount(): AccountInfo | null {
    return this.msalInstance.getActiveAccount();
  }

  getMsalInstance(): PublicClientApplication {
    return this.msalInstance;
  }

  isAuthenticated(): boolean {
    return this.msalInstance.getAllAccounts().length > 0;
  }
}

export const authService = new AuthService();