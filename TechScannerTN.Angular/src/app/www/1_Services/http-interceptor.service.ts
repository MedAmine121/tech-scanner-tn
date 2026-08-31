import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { catchError, Observable, throwError } from "rxjs";
import { NotificationService } from "./notification.service";
import { StorageService } from "./storage.service";
import { Constants } from "../6_Common/constants";
import { Router } from "@angular/router";

@Injectable({
  providedIn: 'root'
})
export class HttpErrorInterceptor implements HttpInterceptor {
  private notificationService = inject(NotificationService);
  private storageService = inject(StorageService);
  private router = inject(Router);

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        switch (error.status) {
          case 401:
            this.storageService.removeLocalStorage(Constants.CONTEXT_KEY);
            if (!window.location.pathname.includes('/login') && !window.location.pathname.includes('/signup')) {
              this.notificationService.showErrorToast('Session expired or invalid, please sign in.');
              this.router.navigate(['/login']);
            }
            break;
          case 403:
            this.notificationService.showErrorToast('You do not have permission to perform this action.');
            break;
          case 0:
            this.notificationService.showErrorToast('Unable to connect to the TechScannerTN server.');
            break;
          default:
            break;
        }
      }
      return throwError(() => error);
    }));
  }
}