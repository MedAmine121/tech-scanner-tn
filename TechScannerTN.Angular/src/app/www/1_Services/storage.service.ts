import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class StorageService {
  constructor() { }

  setLocalStorage(key: string, value: unknown): void {
    try {
      localStorage.setItem(key, JSON.stringify(value));
    } catch (e) {
      console.error('StorageService setLocalStorage error', e);
    }
  }

  getLocalStorage<T = unknown>(key: string): T | null {
    try {
      const item = localStorage.getItem(key);
      return item ? JSON.parse(item) as T : null;
    } catch (e) {
      console.error('StorageService getLocalStorage error', e);
      return null;
    }
  }

  removeLocalStorage(key: string): void {
    try {
      localStorage.removeItem(key);
    } catch (e) {
      console.error('StorageService removeLocalStorage error', e);
    }
  }

  clearLocalStorage(): void {
    try {
      localStorage.clear();
    } catch (e) {
      console.error('StorageService clearLocalStorage error', e);
    }
  }
}
