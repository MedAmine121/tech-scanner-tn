import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { StorageService } from './storage.service';
import { BaseResult } from '../2_Models/common/base-result.model';
import { Constants } from '../6_Common/constants';
import { Context } from '../2_Models/responses/context.model';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private apiUrl = environment.apiUrl;
  private http = inject(HttpClient);
  private storage = inject(StorageService);

  private getAuthHeaders(): HttpHeaders {
    const ctx = this.storage.getLocalStorage<Context>(Constants.CONTEXT_KEY);
    const token = ctx?.token || null;
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

  GetRaw$<T>(endpoint: string, params?: Record<string, any>): Observable<T> {
    const headers = this.getAuthHeaders();
    let httpParams = new HttpParams();
    if (params) {
      Object.keys(params).forEach(key => {
        const val = params[key];
        if (val !== undefined && val !== null && val !== '') {
          httpParams = httpParams.set(key, String(val));
        }
      });
    }
    return this.http.get<T>(`${this.apiUrl}/${endpoint}`, { headers, params: httpParams });
  }

  Post$<T>(endpoint: string, body: any, isAnonymous: boolean = false): Observable<BaseResult<T>> {
    const headers = isAnonymous ? new HttpHeaders({ 'Content-Type': 'application/json' }) : this.getAuthHeaders();
    return this.http.post<BaseResult<T>>(`${this.apiUrl}/${endpoint}`, body, { headers });
  }
}
