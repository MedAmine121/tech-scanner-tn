import { Component, inject, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BaseComponent } from '../../shared/base-component/base-component';
import { UserBLLService } from '../../../4_BLL/user-bll.service';
import { SaveResponse } from '../../../2_Models/common/save-response.model';
import { Constants } from '../../../6_Common/constants';

@Component({
  selector: 'app-logout-component',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './logout-component.html',
  styleUrl: './logout-component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LogoutComponent extends BaseComponent implements OnInit {
  private userBLL = inject(UserBLLService);

  ngOnInit(): void {
    this.userBLL.logout$().subscribe({
      next: (response: SaveResponse | null) => {
        this.storageService.removeLocalStorage(Constants.CONTEXT_KEY);
        this.userService.clear();
        this.notificationService.showSuccessToast('You have been logged out successfully.');
        this.router.navigate(['/']);
      },
      error: () => {
        this.storageService.removeLocalStorage(Constants.CONTEXT_KEY);
        this.userService.clear();
        this.router.navigate(['/']);
      }
    });
  }
}

