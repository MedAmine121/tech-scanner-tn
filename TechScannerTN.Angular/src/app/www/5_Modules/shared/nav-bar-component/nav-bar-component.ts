import { Component, OnInit, HostListener, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { BaseComponent } from '../base-component/base-component';

@Component({
  selector: 'app-nav-bar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './nav-bar-component.html',
  styleUrl: './nav-bar-component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NavBarComponent extends BaseComponent implements OnInit {
  readonly isMenuOpen = signal<boolean>(false);
  readonly showUserDropdown = signal<boolean>(false);

  readonly isAuth = computed<boolean>(() => this.userService.isAuthenticated());
  readonly currentUser = computed(() => this.userService.user());
  readonly userDisplayName = computed<string>(() => this.userService.userDisplayName());
  readonly userInitials = computed<string>(() => this.userService.userInitials());

  ngOnInit(): void {
    if (this.isAuth() && !this.currentUser()) {
      this.userService.fetchCurrentUser();
    }
  }

  toggleMenu(): void {
    this.isMenuOpen.update(v => !v);
  }

  toggleUserDropdown(event: Event): void {
    event.stopPropagation();
    this.showUserDropdown.update(v => !v);
  }

  closeUserDropdown(): void {
    this.showUserDropdown.set(false);
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    this.closeUserDropdown();
  }

  onLogout(): void {
    this.closeUserDropdown();
    this.redirectTo(['/logout']);
  }
}
