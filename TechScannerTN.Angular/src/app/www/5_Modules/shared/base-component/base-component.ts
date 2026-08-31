import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { NotificationService } from '../../../1_Services/notification.service';
import { StorageService } from '../../../1_Services/storage.service';
import { AuthService } from '../../../1_Services/auth.service';
import { UserService } from '../../../1_Services/user.service';
import { Constants } from '../../../6_Common/constants';
import { NavConstants } from '../../../6_Common/nav-constants';

@Component({
  selector: 'app-base-component',
  template: '',
})
export class BaseComponent {
  protected notificationService = inject(NotificationService);
  protected storageService = inject(StorageService);
  protected authService = inject(AuthService);
  protected userService = inject(UserService);
  protected router = inject(Router);

  constants = Constants;
  navConstants = NavConstants;

  redirectTo(route: string[]): void {
    this.router.navigate(route);
  }
}

