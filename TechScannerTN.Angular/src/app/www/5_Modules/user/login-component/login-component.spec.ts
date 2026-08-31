import '@angular/compiler';
import { describe, it, expect } from 'vitest';
import { signal, computed } from '@angular/core';

describe('Signal-based Login Validation', () => {
  it('should validate email and password using signals', () => {
    const email = signal('');
    const password = signal('');

    const isEmailValid = computed(() => {
      const val = email().trim();
      return !!val && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(val);
    });

    const isPasswordValid = computed(() => password().length >= 6);
    const isFormValid = computed(() => isEmailValid() && isPasswordValid());

    expect(isFormValid()).toBe(false);

    email.set('invalid-email');
    password.set('123');
    expect(isEmailValid()).toBe(false);
    expect(isPasswordValid()).toBe(false);
    expect(isFormValid()).toBe(false);

    email.set('amine@techscanner.tn');
    password.set('StrongPassword123');
    expect(isEmailValid()).toBe(true);
    expect(isPasswordValid()).toBe(true);
    expect(isFormValid()).toBe(true);
  });
});

