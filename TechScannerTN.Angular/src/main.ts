import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import { provideNgxStripe } from 'ngx-stripe';
import { environment } from './environments/environment';

bootstrapApplication(App, {
  ...appConfig,
  providers: [
    ...appConfig.providers,
    provideNgxStripe(environment.stripePublicKey)
  ]
})
  .catch((err) => console.error(err));