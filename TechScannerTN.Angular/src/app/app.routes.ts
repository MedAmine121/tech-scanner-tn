import { Routes } from '@angular/router';
import { HomeComponent } from './www/5_Modules/home/home-component/home-component';
import { LoginComponent } from './www/5_Modules/user/login-component/login-component';
import { SignupComponent } from './www/5_Modules/user/signup-component/signup-component';
import { LogoutComponent } from './www/5_Modules/user/logout-component/logout-component';
import { guestGuard } from './www/7_Guards/guest.guard';

export const routes: Routes = [
  {
    path: '',
    component: HomeComponent
  },
  {
    path: 'products',
    loadComponent: () => import('./www/5_Modules/products/products-list/products-list.component').then(m => m.ProductsListComponent)
  },
  {
    path: 'login',
    component: LoginComponent,
    canActivate: [guestGuard]
  },
  {
    path: 'signup',
    component: SignupComponent,
    canActivate: [guestGuard]
  },
  {
    path: 'logout',
    component: LogoutComponent
  },
  {
    path: '**',
    redirectTo: ''
  }
];

