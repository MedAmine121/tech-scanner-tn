import { Component, OnInit, ChangeDetectionStrategy, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BaseComponent } from '../../shared/base-component/base-component';
import { LandingPageComponent } from '../../landing-page/landing-page.component';

@Component({
  selector: 'app-home-component',
  standalone: true,
  imports: [CommonModule, LandingPageComponent],
  templateUrl: './home-component.html',
  styleUrl: './home-component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomeComponent extends BaseComponent implements OnInit {
  readonly isAuth = computed<boolean>(() => this.userService.isAuthenticated());

  ngOnInit(): void {
    if (this.isAuth() && !this.userService.user()) {
      this.userService.fetchCurrentUser();
    }
  }
}

