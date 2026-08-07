import { inject, Injectable } from "@angular/core";
import { ToastrService } from "ngx-toastr";
@Injectable({
    providedIn: 'root'
})
export class NotificationService {
    private toastr = inject(ToastrService);
    showSuccessToast(message: string, title = 'Success'): void{
        this.toastr.success(message, title);
    }
    showErrorToast(message: string, title = 'Error'): void{
        this.toastr.error(message);
    }
    showWarningToast(message: string, title = 'Warning'): void{
        this.toastr.warning(message);
    }
}