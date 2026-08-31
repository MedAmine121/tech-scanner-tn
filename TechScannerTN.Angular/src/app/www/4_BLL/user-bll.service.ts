import { inject, Injectable } from "@angular/core";
import { UserDALService } from "../3_DAL/user-dal.service";
import { LoginUserRequest } from "../2_Models/requests/login-request.model";
import { CreateUserRequest } from "../2_Models/requests/create-user-request.model";
import { map, Observable } from "rxjs";
import { Context } from "../2_Models/responses/context.model";
import { BaseResult } from "../2_Models/common/base-result.model";
import { ResultType } from "../2_Models/common/result-type.model";
import { BaseBLLService } from "./base-bll.service";
import { SaveResponse } from "../2_Models/common/save-response.model";

@Injectable({
  providedIn: 'root'
})
export class UserBLLService extends BaseBLLService {
  private userDALService = inject(UserDALService);

  login$(request: LoginUserRequest): Observable<Context | null> {
    return this.userDALService.login$(request).pipe(map((response: BaseResult<Context>) => {
      if (response && response.resultType === ResultType.Success && response.model) {
        return response.model;
      }
      if (response && response.resultType !== ResultType.Success) {
        this.handleResult(response);
      }
      return null;
    }));
  }

  signup$(request: CreateUserRequest): Observable<Context | null> {
    return this.userDALService.signup$(request).pipe(map((response: BaseResult<Context>) => {
      if (response && response.resultType === ResultType.Success && response.model) {
        return response.model;
      }
      if (response && response.resultType !== ResultType.Success) {
        this.handleResult(response);
      }
      return null;
    }));
  }

  logout$(): Observable<SaveResponse | null> {
    return this.userDALService.logout$().pipe(map((response: BaseResult<SaveResponse>) => {
      if (response && response.resultType === ResultType.Success && response.model) {
        return response.model;
      }
      if (response && response.resultType !== ResultType.Success) {
        this.handleResult(response);
      }
      return null;
    }));
  }

  fetchUser$(): Observable<Context | null> {
    return this.userDALService.fetchUser$().pipe(map((response: BaseResult<Context>) => {
      if (response && response.resultType === ResultType.Success && response.model) {
        return response.model;
      }
      if (response && response.resultType !== ResultType.Success) {
        this.handleResult(response);
      }
      return null;
    }));
  }
}

