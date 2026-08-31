import { inject, Injectable } from "@angular/core";
import { ApiService } from "../1_Services/api.service";
import { Observable } from "rxjs";
import { BaseResult } from "../2_Models/common/base-result.model";

@Injectable({
  providedIn: 'root'
})
export class BaseDALService {
  controller = '';
  protected apiService = inject(ApiService);

  SendPost$<T>(action: string, request: unknown, isAnonymous = false): Observable<BaseResult<T>> {
    const endpoint = this.controller ? `${this.controller}/${action}` : action;
    return this.apiService.Post$<T>(endpoint, request, isAnonymous);
  }

  SendGet$<T>(action: string, isAnonymous = false): Observable<BaseResult<T>> {
    const endpoint = this.controller ? `${this.controller}/${action}` : action;
    return this.apiService.Get$<T>(endpoint, isAnonymous);
  }
}

