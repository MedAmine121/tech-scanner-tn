import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { catchError, Observable, throwError } from "rxjs";
import { NotificationService } from "./notification.service";
@Injectable({
    providedIn: 'root'
})
export class HttpErrorInterceptor implements HttpInterceptor {
    private notificationService = inject(NotificationService);
    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        return next.handle(req).pipe(catchError((error: unknown) => {
            if(error instanceof HttpErrorResponse){
                switch(error.status){
                    case 401:
                        this.notificationService.showErrorToast('Session Invalid, please login.');
                        window.location.href = '/login';
                        break;
                    case 403:
                        this.notificationService.showErrorToast('You don’t have permission to perform this action.');
                        break;
                    default:
                        this.notificationService.showErrorToast('Server error. Please try again later');
                }
            }
            return throwError(() => error);
        }))
    }
    
}