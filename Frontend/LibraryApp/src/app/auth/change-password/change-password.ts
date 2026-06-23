import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-change-password',
  templateUrl: './change-password.html',
  styleUrls: ['./change-password.css'],
  standalone: false
})
export class ChangePassword {
  passwordForm: FormGroup;
  errorMessage: string = '';
  successMessage: string = '';
  isAdmin: boolean = false;
  isAdminOverride: boolean = false;

  constructor(private fb: FormBuilder, private authService: AuthService) {
    this.isAdmin = this.authService.isAdmin();
    
    this.passwordForm = this.fb.group({
      EmpId: [this.authService.getEmpId() || ''],
      OldPassword: [''],
      NewPassword: ['', [
        Validators.required, 
        Validators.minLength(8),
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/)
      ]]
    });
  }

  toggleAdminOverride(event: any) {
    this.isAdminOverride = event.target.checked;
    if (!this.isAdminOverride) {
      this.passwordForm.get('EmpId')?.setValue(this.authService.getEmpId() || '');
    }
  }

  onSubmit() {
    if (this.passwordForm.invalid) {
      this.errorMessage = "Please enter valid passwords.";
      return;
    }

    const formValue = this.passwordForm.value;

    if (this.isAdminOverride) {
      if (!formValue.EmpId) {
        this.errorMessage = "Employee ID is required for Admin Override.";
        return;
      }
      this.authService.adminChangePassword({
        empId: formValue.EmpId,
        newPassword: formValue.NewPassword
      }).subscribe({
        next: (res: any) => {
          this.successMessage = res.message || "Password changed successfully for employee.";
          this.errorMessage = '';
          this.passwordForm.reset();
          this.isAdminOverride = false;
        },
        error: (err: any) => {
          if (err.error?.errors) {
            const firstErrorKey = Object.keys(err.error.errors)[0];
            this.errorMessage = err.error.errors[firstErrorKey][0];
          } else {
            this.errorMessage = err.error?.message || "Failed to change password.";
          }
          this.successMessage = '';
        }
      });
    } else {
      if (!formValue.OldPassword) {
        this.errorMessage = "Old password is required.";
        return;
      }
      if (!formValue.EmpId) {
        this.errorMessage = "Employee ID is required.";
        return;
      }
      this.authService.changePassword({
        EmpId: formValue.EmpId,
        OldPassword: formValue.OldPassword,
        NewPassword: formValue.NewPassword
      }).subscribe({
        next: (res: any) => {
          this.successMessage = res.message || "Password changed successfully.";
          this.errorMessage = '';
          this.passwordForm.reset();
        },
        error: (err: any) => {
          if (err.error?.errors) {
            const firstErrorKey = Object.keys(err.error.errors)[0];
            this.errorMessage = err.error.errors[firstErrorKey][0];
          } else {
            this.errorMessage = err.error?.message || "Failed to change password.";
          }
          this.successMessage = '';
        }
      });
    }
  }
}
