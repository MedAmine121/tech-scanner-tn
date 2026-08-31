import { Component, inject, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { BaseComponent } from '../../shared/base-component/base-component';
import { UserBLLService } from '../../../4_BLL/user-bll.service';
import { LoginUserRequest } from '../../../2_Models/requests/login-request.model';
import { Context } from '../../../2_Models/responses/context.model';
import { Constants } from '../../../6_Common/constants';

@Component({
  selector: 'app-login-component',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './login-component.html',
  styleUrl: './login-component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginComponent extends BaseComponent {
  private userBLL = inject(UserBLLService);

  readonly email = signal<string>('');
  readonly password = signal<string>('');
  readonly rememberMe = signal<boolean>(true);

  readonly emailTouched = signal<boolean>(false);
  readonly passwordTouched = signal<boolean>(false);

  readonly isLoading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly showPassword = signal<boolean>(false);

  readonly isEmailValid = computed<boolean>(() => {
    const val = this.email().trim();
    return !!val && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(val);
  });

  readonly isPasswordValid = computed<boolean>(() => {
    return this.password().length >= 6;
  });

  readonly emailHasError = computed<boolean>(() => {
    return this.emailTouched() && !this.isEmailValid();
  });

  readonly passwordHasError = computed<boolean>(() => {
    return this.passwordTouched() && !this.isPasswordValid();
  });

  readonly isFormValid = computed<boolean>(() => {
    return this.isEmailValid() && this.isPasswordValid();
  });

  onEmailChange(val: string): void {
    this.email.set(val);
  }

  onPasswordChange(val: string): void {
    this.password.set(val);
  }

  onRememberMeChange(val: boolean): void {
    this.rememberMe.set(val);
  }

  markEmailTouched(): void {
    this.emailTouched.set(true);
  }

  markPasswordTouched(): void {
    this.passwordTouched.set(true);
  }

  togglePasswordVisibility(): void {
    this.showPassword.update(v => !v);
  }

  onLogin(): void {
    this.errorMessage.set(null);
    this.emailTouched.set(true);
    this.passwordTouched.set(true);

    if (!this.isFormValid()) {
      return;
    }

    this.isLoading.set(true);

    const request: LoginUserRequest = {
      email: this.email().trim(),
      password: this.password()
    };

    this.userBLL.login$(request).subscribe({
      next: (context: Context | null) => {
        this.isLoading.set(false);
        if (context) {
          this.notificationService.showSuccessToast('Welcome back to TechScannerTN!');
          this.storageService.setLocalStorage(Constants.CONTEXT_KEY, context);
          this.userService.setContext(context);
          this.router.navigate(['/']);
        } else {
          this.errorMessage.set('Invalid email or password. Please check your credentials and try again.');
        }
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMessage.set('Unable to authenticate. Please check your network or try again.');
      }
    });
  }
}

