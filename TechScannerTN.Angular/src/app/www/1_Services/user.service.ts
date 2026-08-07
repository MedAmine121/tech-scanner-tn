import { Injectable, inject, signal } from '@angular/core';
import { ApiService } from './api.service';
import { StorageService } from './storage.service';

export interface AppContext {
  fullName?: string;
  profilePictureUrl?: string;
  expires?: string;
  token?: string;
  [key: string]: any;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  user = signal<AppContext | null>(null);
  private api = inject(ApiService);
  private storage = inject(StorageService);

  constructor() {
    const stored = this.storage.getLocalStorage('app_context') as AppContext | null;
    if (stored) {
      this.user.set(stored);
    }
  }

  isAuthenticated(): boolean {
    const ctx = this.user();
    if (!ctx) return false;
    if (!ctx.expires) return true;
    return new Date(ctx.expires) > new Date();
  }

  fetchCurrentUser(): void {
    // Generic endpoint name; adapt to your API (e.g. 'auth/context' or 'users/me')
    this.api.Get$<AppContext>('auth/context', false).subscribe({
      next: (res) => {
        if (res?.success && res.data) {
          this.user.set(res.data);
          this.storage.setLocalStorage('app_context', res.data);
        }
      },
      error: () => {
        // swallow or notify as appropriate
      }
    });
  }

  setContext(ctx: AppContext | null): void {
    this.user.set(ctx);
    if (ctx) {
      this.storage.setLocalStorage('app_context', ctx);
    } else {
      this.storage.removeLocalStorage('app_context');
    }
  }

  clear(): void {
    this.setContext(null);
  }

  get showInitials(): boolean {
    const ctx = this.user();
    return !!ctx && !ctx.profilePictureUrl;
  }

  get userAvatar(): string {
    const ctx = this.user();
    if (!ctx) return '';
    if (!ctx.profilePictureUrl) return this.getAvatarInitials(ctx.fullName || '');
    let pic = ctx.profilePictureUrl;
    // strip any wwwroot/ prefix if present
    pic = pic.replace(/^\/wwwroot\//, '').replace(/^wwwroot\//, '');
    // do not assume apiUrl prefix here; let callers resolve if needed
    return pic;
  }

  private getAvatarInitials(name: string): string {
    const initials = name
      .split(' ')
      .map((n) => n.charAt(0).toUpperCase())
      .join('');
    return initials || '👤';
  }
}