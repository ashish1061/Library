import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.html',
  styleUrls: ['./register.css'],
  standalone: false
})
export class Register {
  registerForm: FormGroup;
  errorMessage: string = '';
  successMessage: string = '';

  constructor(private fb: FormBuilder, private authService: AuthService, private router: Router) {
    this.registerForm = this.fb.group({
      EmpID: ['', Validators.required],
      EmpName: ['', Validators.required],
      Email: ['', [Validators.required, Validators.email]],
      Password: ['', [
        Validators.required, 
        Validators.minLength(8), 
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/)
      ]],
      Mobile: ['', [Validators.required, Validators.pattern('^[0-9+\\- ]{10,15}$')]],
      Department: ['', [Validators.required, Validators.pattern('^\\s*\\S+(?:\\s+\\S+)+\\s*$')]],
      Designation: ['', [Validators.required, Validators.pattern('^\\s*\\S+(?:\\s+\\S+)+\\s*$')]]
    });
  }

  onSubmit() {
    if (this.registerForm.invalid) {
      this.errorMessage = "Please fill out the form correctly.";
      return;
    }

    this.authService.register(this.registerForm.value).subscribe({
      next: (res: any) => {
        this.successMessage = res.message || "User registered successfully.";
        this.errorMessage = '';
        this.registerForm.reset();
      },
      error: (err: any) => {
        if (err.error?.errors) {
          const firstErrorKey = Object.keys(err.error.errors)[0];
          this.errorMessage = err.error.errors[firstErrorKey][0];
        } else {
          this.errorMessage = err.error?.message || "Registration failed.";
        }
        this.successMessage = '';
      }
    });
  }
}
