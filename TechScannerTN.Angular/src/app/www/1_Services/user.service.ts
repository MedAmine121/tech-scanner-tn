import { Injectable, inject, signal, computed } from '@angular/core';
import { ApiService } from './api.service';
import { StorageService } from './storage.service';
import { Context } from '../2_Models/responses/context.model';
import { Constants } from '../6_Common/constants';
import { ResultType } from '../2_Models/common/result-type.model';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  user = signal<Context | null>(null);
  private api = inject(ApiService);
  private storage = inject(StorageService);

  readonly isAuthenticated = computed<boolean>(() => {
    const ctx = this.user();
    if (!ctx || !ctx.token) return false;
    if (!ctx.expires) return true;
    return new Date(ctx.expires) > new Date();
  });

  readonly userDisplayName = computed<string>(() => {
    const u = this.user();
    return u?.fullName || u?.email?.split('@')[0] || 'User';
  });

  readonly userInitials = computed<string>(() => {
    return this.getAvatarInitials(this.userDisplayName());
  });

  readonly showInitials = computed<boolean>(() => {
    const ctx = this.user();
    return !!ctx && !ctx.profilePictureUrl;
  });

  readonly userAvatar = computed<string>(() => {
    const ctx = this.user();
    if (!ctx) return '';
    if (!ctx.profilePictureUrl) return this.userInitials();
    let pic = ctx.profilePictureUrl;
    pic = pic.replace(/^\/wwwroot\//, '').replace(/^wwwroot\//, '');
    return pic;
  });

  constructor() {
    const stored = this.storage.getLocalStorage<Context>(Constants.CONTEXT_KEY);
    if (stored) {
      this.user.set(stored);
    }
  }

  fetchCurrentUser(): void {
    this.api.Get$<Context>('user/fetch', false).subscribe({
      next: (res) => {
        if (res && res.resultType === ResultType.Success && res.model) {
          const current = this.user();
          const updated: Context = {
            ...res.model,
            token: current?.token || res.model.token
          };
          this.setContext(updated);
        }
      },
      error: () => {
        // Handled by HTTP interceptor
      }
    });
  }

  setContext(ctx: Context | null): void {
    this.user.set(ctx);
    if (ctx) {
      this.storage.setLocalStorage(Constants.CONTEXT_KEY, ctx);
    } else {
      this.storage.removeLocalStorage(Constants.CONTEXT_KEY);
    }
  }

  clear(): void {
    this.setContext(null);
  }

  getAvatarInitials(name: string): string {
    const initials = (name || '')
      .split(' ')
      .filter(Boolean)
      .map((n) => n.charAt(0).toUpperCase())
      .join('');
    return initials.substring(0, 2) || 'TN';
  }
}

