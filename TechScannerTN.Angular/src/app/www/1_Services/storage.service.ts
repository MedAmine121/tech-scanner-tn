import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class StorageService {
n    constructor() { }
n    // LocalStorage methods
    setLocalStorage(key: string, value: unknown): void {
        localStorage.setItem(key, JSON.stringify(value));
    }
n    getLocalStorage(key: string): unknown {
        const item = localStorage.getItem(key);
        return item ? JSON.parse(item) : null;
    }
n    removeLocalStorage(key: string): void {
        localStorage.removeItem(key);
    }
n    clearLocalStorage(): void {
        localStorage.clear();
    }
n    // SessionStorage methods
    setSessionStorage(key: string, value: unknown): void {
        sessionStorage.setItem(key, JSON.stringify(value));
    }
n    getSessionStorage(key: string): unknown {
        const item = sessionStorage.getItem(key);
        return item ? JSON.parse(item) : null;
    }
n    removeSessionStorage(key: string): void {
        sessionStorage.removeItem(key);
    }
n    clearSessionStorage(): void {
        sessionStorage.clear();
    }
}