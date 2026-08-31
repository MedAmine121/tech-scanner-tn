import { inject, Injectable } from "@angular/core";
import { ToastrService } from "ngx-toastr";

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private toastr = inject(ToastrService);

  showSuccessToast(message: string, title: string = 'Success'): void {
    this.toastr.success(message, title);
  }

  showErrorToast(message: string, title: string = 'Error'): void {
    this.toastr.error(message, title);
  }

  showWarningToast(message: string, title: string = 'Warning'): void {
    this.toastr.warning(message, title);
  }

  showInfoToast(message: string, title: string = 'Info'): void {
    this.toastr.info(message, title);
  }
}