import { inject, Injectable } from "@angular/core";
import { StorageService } from "./storage.service";
import { Constants } from "../6_Common/constants";
import { Context } from "../2_Models/responses/context.model";
import { HttpHeaders } from "@angular/common/http";

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private storageService = inject(StorageService);

  isAuthenticated(): boolean {
    const context = this.storageService.getLocalStorage<Context>(Constants.CONTEXT_KEY);
    if (!context || !context.token) {
      return false;
    }
    if (context.expires) {
      const expiryDate = new Date(context.expires);
      if (expiryDate < new Date()) {
        this.storageService.removeLocalStorage(Constants.CONTEXT_KEY);
        return false;
      }
    }
    return true;
  }

  isAdmin(): boolean {
    const context = this.getContext();
    return !!context && context.role === 1;
  }

  getToken(): string | null {
    const context = this.getContext();
    return context?.token || null;
  }

  getAuthHeaders(): HttpHeaders {
    const token = this.getToken();
    if (token) {
      return new HttpHeaders({
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      });
    }
    return new HttpHeaders({ 'Content-Type': 'application/json' });
  }

  getContext(): Context | null {
    return this.storageService.getLocalStorage<Context>(Constants.CONTEXT_KEY);
  }
}

