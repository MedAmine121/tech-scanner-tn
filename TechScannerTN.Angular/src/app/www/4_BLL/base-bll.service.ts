import { inject, Injectable } from "@angular/core";
import { BaseResult } from "../2_Models/common/base-result.model";
import { ResultType } from "../2_Models/common/result-type.model";
import { NotificationService } from "../1_Services/notification.service";

@Injectable({
  providedIn: 'root'
})
export class BaseBLLService {
  protected notificationService = inject(NotificationService);

  handleResult(response: BaseResult<any>): void {
    if (response.resultType === ResultType.Error) {
      this.notificationService.showErrorToast(response?.message || 'An error has occurred. Please try again later.');
    } else if (response.resultType === ResultType.Fail) {
      this.notificationService.showErrorToast(response?.message || 'Action failed.');
    } else if (response.resultType === ResultType.BadRequest) {
      let message = response.message ?? '';
      message = message.replace("Validation failed: \r\n -- ", "");
      message = message.replace("Severity: Error", "");
      this.notificationService.showErrorToast(message || 'Invalid input.');
    } else if (response.resultType === ResultType.Unauthorized) {
      this.notificationService.showErrorToast(response?.message || 'Unauthorized access.');
    }
  }
}

