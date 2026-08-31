import { Injectable } from "@angular/core";
import { LoginUserRequest } from "../2_Models/requests/login-request.model";
import { CreateUserRequest } from "../2_Models/requests/create-user-request.model";
import { BaseResult } from "../2_Models/common/base-result.model";
import { Context } from "../2_Models/responses/context.model";
import { SaveResponse } from "../2_Models/common/save-response.model";
import { Observable } from "rxjs";
import { BaseDALService } from "./base-dal.service";

@Injectable({
  providedIn: 'root'
})
export class UserDALService extends BaseDALService {
  override readonly controller = 'user';

  login$(request: LoginUserRequest): Observable<BaseResult<Context>> {
    return this.SendPost$<Context>('login', request, true);
  }

  logout$(): Observable<BaseResult<SaveResponse>> {
    return this.SendPost$<SaveResponse>('logout', {});
  }

  signup$(request: CreateUserRequest): Observable<BaseResult<Context>> {
    return this.SendPost$<Context>('signup', request, true);
  }

  fetchUser$(): Observable<BaseResult<Context>> {
    return this.SendGet$<Context>('fetch');
  }
}

