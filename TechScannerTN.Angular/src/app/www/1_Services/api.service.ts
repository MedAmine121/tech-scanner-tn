import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { StorageService } from './storage.service';
import { BaseResult } from '../2_Models/common/base-result.model';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private apiUrl = environment.apiUrl;
  private http = inject(HttpClient);
  private storage = inject(StorageService);

  private getAuthHeaders(): HttpHeaders {
    const ctx = this.storage.getLocalStorage('app_context') as any;
    const token = ctx?.token || ctx?.accessToken || null;
    if (token) {
      return new HttpHeaders({
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json'
      });
    }
    return new HttpHeaders({ 'Content-Type': 'application/json' });
  }

  Get$<T>(endpoint: string, isAnonymous: boolean = false): Observable<BaseResult<T>> {
    const headers = isAnonymous ? new HttpHeaders({ 'Content-Type': 'application/json' }) : this.getAuthHeaders();
    return this.http.get<BaseResult<T>>(`${this.apiUrl}/${endpoint}`, { headers });
  }

  Post$<T>(endpoint: string, body: any, isAnonymous: boolean = false): Observable<BaseResult<T>> {
    const headers = isAnonymous ? new HttpHeaders({ 'Content-Type': 'application/json' }) : this.getAuthHeaders();
    return this.http.post<BaseResult<T>>(`${this.apiUrl}/${endpoint}`, body, { headers });
  }
}