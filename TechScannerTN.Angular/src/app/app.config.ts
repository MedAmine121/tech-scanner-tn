import { ApplicationConfig, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter, withViewTransitions } from '@angular/router';
import { provideHttpClient, withInterceptorsFromDi, HTTP_INTERCEPTORS } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideToastr } from 'ngx-toastr';
import { routes } from './app.routes';
import { HttpErrorInterceptor } from './www/1_Services/http-interceptor.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideRouter(routes, withViewTransitions()),
    provideHttpClient(withInterceptorsFromDi()),
    {
      provide: HTTP_INTERCEPTORS,
      useClass: HttpErrorInterceptor,
      multi: true
    },
    provideAnimationsAsync(),
    provideToastr({
      timeOut: 3500,
      positionClass: 'toast-bottom-right',
      preventDuplicates: true,
      progressBar: true,
      closeButton: true
    })
  ]
};

