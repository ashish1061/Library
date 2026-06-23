import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { Router } from '@angular/router';
import { Store } from '@ngxs/store';
import { jwtDecode } from 'jwt-decode';
import { SetTokens } from '../../store/auth.state';
import * as CryptoJS from 'crypto-js';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-login',
  standalone: false,
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login implements OnInit {
  loginForm!: FormGroup;
  otpForm!: FormGroup;
  forceChangePasswordForm!: FormGroup;
  isLoading = false;
  mfaRequired = false;
  requirePasswordChange = false;
  emailForMfa = '';
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private store: Store,
    private cdr: ChangeDetectorRef,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
    this.otpForm = this.fb.group({
      otp: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(6)]]
    });
    this.forceChangePasswordForm = this.fb.group({
      newPassword: ['', [
        Validators.required, 
        Validators.minLength(8), 
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/)
      ]]
    });
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const rawPayload = this.loginForm.value;
    const jsonString = JSON.stringify(rawPayload);

    // Cryptographic settings matching C# backend decryption
    const key = CryptoJS.enc.Utf8.parse('JslLibrarySecretKeyForLogin2026!');
    const iv = CryptoJS.enc.Utf8.parse('JslLibraryLogIv1');

    const encrypted = CryptoJS.AES.encrypt(jsonString, key, {
      iv: iv,
      mode: CryptoJS.mode.CBC,
      padding: CryptoJS.pad.Pkcs7
    }).toString();

    const payload = {
      encryptedData: encrypted
    };

    this.authService.login(payload).subscribe({
      next: (res: any) => {
        this.isLoading = false;
        console.log('Login Response:', res);
        if (res && res.mfaRequired) {
          console.log('MFA Required evaluated to true, updating UI');
          this.mfaRequired = true;
          this.emailForMfa = res.email;
          this.cdr.detectChanges();
          return;
        }

        this.handleLoginSuccess(res);
      },
      error: (err) => {
        console.error('Login error', err);
        if (err.status === 403 && err.error?.requirePasswordChange) {
          this.requirePasswordChange = true;
          this.emailForMfa = err.error.email;
          this.errorMessage = err.error.message;
          this.isLoading = false;
          this.cdr.detectChanges();
          return;
        }
        if (err.error && err.error.message) {
          this.errorMessage = err.error.message;
        } else {
          this.errorMessage = 'Login failed. Please check your credentials.';
        }
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  onOtpSubmit(): void {
    if (this.otpForm.invalid) {
      this.otpForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const otpValue = this.otpForm.value.otp;

    this.authService.verifyOtp(this.emailForMfa, otpValue).subscribe({
      next: (res: any) => {
        this.isLoading = false;
        this.handleLoginSuccess(res);
      },
      error: (err) => {
        console.error('OTP verification error', err);
        if (err.error && err.error.message) {
          this.errorMessage = err.error.message;
        } else {
          this.errorMessage = 'Verification failed. Invalid or expired OTP.';
        }
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  handleLoginSuccess(res: any): void {
    // Dispatch NGXS Action to save tokens
    this.store.dispatch(new SetTokens({
      accessToken: res.accessToken,
      refreshToken: res.refreshToken
    })).subscribe(() => {
      // Decode token to redirect based on role
      try {
        const decoded: any = jwtDecode(res.accessToken);
        const role = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || decoded.role;
        
        if (role === 'Admin') {
          this.router.navigate(['/library/dashboard']);
        } else {
          this.router.navigate(['/library/book-search']);
        }
      } catch(e) {
        this.router.navigate(['/library/book-search']);
      }
    });
  }

  onForceChangePasswordSubmit(): void {
    if (this.forceChangePasswordForm.invalid) {
      this.forceChangePasswordForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const payload = {
      email: this.emailForMfa,
      oldPassword: 'Library@123',
      newPassword: this.forceChangePasswordForm.value.newPassword
    };

    this.authService.forceChangePassword(payload).subscribe({
      next: (res: any) => {
        this.isLoading = false;
        this.requirePasswordChange = false;
        this.errorMessage = '';
        this.toastService.success('Password changed successfully! Please log in with your new password.');
        this.loginForm.reset();
        this.forceChangePasswordForm.reset();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Force Change Password error', err);
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Failed to change password.';
        this.cdr.detectChanges();
      }
    });
  }
}
