import { Component, inject, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { BaseComponent } from '../../shared/base-component/base-component';
import { UserBLLService } from '../../../4_BLL/user-bll.service';
import { CreateUserRequest } from '../../../2_Models/requests/create-user-request.model';
import { Context } from '../../../2_Models/responses/context.model';
import { Constants } from '../../../6_Common/constants';

@Component({
  selector: 'app-signup-component',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './signup-component.html',
  styleUrl: './signup-component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SignupComponent extends BaseComponent {
  private userBLL = inject(UserBLLService);

  readonly fullName = signal<string>('');
  readonly email = signal<string>('');
  readonly address = signal<string>('');
  readonly password = signal<string>('');
  readonly confirmPassword = signal<string>('');
  readonly agreedToTerms = signal<boolean>(false);

  readonly fullNameTouched = signal<boolean>(false);
  readonly emailTouched = signal<boolean>(false);
  readonly addressTouched = signal<boolean>(false);
  readonly passwordTouched = signal<boolean>(false);
  readonly confirmPasswordTouched = signal<boolean>(false);
  readonly termsTouched = signal<boolean>(false);

  readonly isLoading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly showPassword = signal<boolean>(false);
  readonly showConfirmPassword = signal<boolean>(false);

  readonly isFullNameValid = computed<boolean>(() => this.fullName().trim().length >= 2);
  readonly isEmailValid = computed<boolean>(() => {
    const val = this.email().trim();
    return !!val && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(val);
  });
  readonly isAddressValid = computed<boolean>(() => this.address().trim().length > 0);
  readonly isPasswordValid = computed<boolean>(() => this.password().length >= 8);
  readonly isPasswordMatch = computed<boolean>(() => !!this.password() && this.password() === this.confirmPassword());
  readonly isTermsValid = computed<boolean>(() => this.agreedToTerms());

  readonly fullNameHasError = computed<boolean>(() => this.fullNameTouched() && !this.isFullNameValid());
  readonly emailHasError = computed<boolean>(() => this.emailTouched() && !this.isEmailValid());
  readonly addressHasError = computed<boolean>(() => this.addressTouched() && !this.isAddressValid());
  readonly passwordHasError = computed<boolean>(() => this.passwordTouched() && !this.isPasswordValid());
  readonly confirmPasswordHasError = computed<boolean>(() => this.confirmPasswordTouched() && !this.isPasswordMatch());
  readonly termsHasError = computed<boolean>(() => this.termsTouched() && !this.isTermsValid());

  readonly isFormValid = computed<boolean>(() =>
    this.isFullNameValid() &&
    this.isEmailValid() &&
    this.isAddressValid() &&
    this.isPasswordValid() &&
    this.isPasswordMatch() &&
    this.isTermsValid()
  );

  onFullNameChange(val: string): void { this.fullName.set(val); }
  onEmailChange(val: string): void { this.email.set(val); }
  onAddressChange(val: string): void { this.address.set(val); }
  onPasswordChange(val: string): void { this.password.set(val); }
  onConfirmPasswordChange(val: string): void { this.confirmPassword.set(val); }
  onAgreedToTermsChange(val: boolean): void { this.agreedToTerms.set(val); }

  markFullNameTouched(): void { this.fullNameTouched.set(true); }
  markEmailTouched(): void { this.emailTouched.set(true); }
  markAddressTouched(): void { this.addressTouched.set(true); }
  markPasswordTouched(): void { this.passwordTouched.set(true); }
  markConfirmPasswordTouched(): void { this.confirmPasswordTouched.set(true); }
  markTermsTouched(): void { this.termsTouched.set(true); }

  togglePasswordVisibility(): void { this.showPassword.update(v => !v); }
  toggleConfirmPasswordVisibility(): void { this.showConfirmPassword.update(v => !v); }

  onSignup(): void {
    this.errorMessage.set(null);
    this.fullNameTouched.set(true);
    this.emailTouched.set(true);
    this.addressTouched.set(true);
    this.passwordTouched.set(true);
    this.confirmPasswordTouched.set(true);
    this.termsTouched.set(true);

    if (!this.isFormValid()) {
      return;
    }

    this.isLoading.set(true);

    const request: CreateUserRequest = {
      fullName: this.fullName().trim(),
      email: this.email().trim(),
      address: this.address().trim(),
      password: this.password()
    };

    this.userBLL.signup$(request).subscribe({
      next: (context: Context | null) => {
        this.isLoading.set(false);
        if (context) {
          this.notificationService.showSuccessToast('Your account was created successfully! Welcome to TechScannerTN.');
          this.storageService.setLocalStorage(Constants.CONTEXT_KEY, context);
          this.userService.setContext(context);
          this.router.navigate(['/']);
        } else {
          this.errorMessage.set('Registration failed. The email address may already be in use.');
        }
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMessage.set('An error occurred during account creation. Please try again.');
      }
    });
  }
}

