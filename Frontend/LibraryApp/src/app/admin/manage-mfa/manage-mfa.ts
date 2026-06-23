import { Component, OnInit } from '@angular/core';
import { EmployeeService } from '../../core/services/employee.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-manage-mfa',
  standalone: false,
  templateUrl: './manage-mfa.html',
  styleUrl: './manage-mfa.css',
})
export class ManageMfa implements OnInit {
  employees: any[] = [];
  isLoading = false;
  searchQuery = '';
  currentPage = 1;
  pageSize = 20;

  // Selection for bulk operations
  selectedEmpIds = new Set<string>();

  constructor(
    private employeeService: EmployeeService,
    private toastService: ToastService
  ) {}

  ngOnInit() {
    const cached = localStorage.getItem('cached_employees_raw');
    if (cached) {
      try {
        const rawList = JSON.parse(cached);
        if (Array.isArray(rawList)) {
          this.employees = this.mapEmployees(rawList);
        }
      } catch (e) {
        console.error('Failed to parse cached employees', e);
      }
    }
    this.loadEmployees();
  }

  mapEmployees(rawList: any[]): any[] {
    return rawList.map((emp: any) => {
      let path = emp.imagePath || emp.ImagePath || null;
      if (path && !path.startsWith('http') && !path.startsWith('data:')) {
        path = `https://localhost:7004${path}`;
      }
      // The database column defaults to true. Let's default true if null/undefined.
      const isMfaEnabled = emp.isMfaEnabled !== false && emp.IsMfaEnabled !== false;
      return {
        ...emp,
        empID: emp.empID || emp.EmpID || '',
        empName: emp.empName || emp.EmpName || '',
        emailid: emp.emailid || emp.email || '',
        imagePath: path,
        department: emp.department || emp.Department || 'N/A',
        designation: emp.designation || emp.Designation || 'N/A',
        isMfaEnabled: isMfaEnabled
      };
    });
  }

  loadEmployees() {
    this.isLoading = this.employees.length === 0;
    this.employeeService.getAllEmployees().subscribe({
      next: (res: any) => {
        this.employees = this.mapEmployees(res);
        localStorage.setItem('cached_employees_raw', JSON.stringify(res));
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load employees', err);
        this.toastService.error('Failed to load employees.');
        this.isLoading = false;
      }
    });
  }

  get filteredAndPagedEmployees() {
    let result = this.employees;
    
    if (this.searchQuery) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(emp => 
        emp.empName.toLowerCase().includes(q) ||
        emp.empID.toLowerCase().includes(q) ||
        emp.emailid.toLowerCase().includes(q) ||
        emp.department.toLowerCase().includes(q) ||
        emp.designation.toLowerCase().includes(q)
      );
    }
    
    const startIndex = (this.currentPage - 1) * this.pageSize;
    return result.slice(startIndex, startIndex + this.pageSize);
  }

  get totalPages() {
    let result = this.employees;
    if (this.searchQuery) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(emp => 
        emp.empName.toLowerCase().includes(q) ||
        emp.empID.toLowerCase().includes(q) ||
        emp.emailid.toLowerCase().includes(q)
      );
    }
    return Math.ceil(result.length / this.pageSize) || 1;
  }

  // Stats Counters
  get totalUsersCount() {
    return this.employees.length;
  }

  get mfaEnabledCount() {
    return this.employees.filter(e => e.isMfaEnabled).length;
  }

  get mfaDisabledCount() {
    return this.employees.filter(e => !e.isMfaEnabled).length;
  }

  // Toggle MFA for single user
  onToggleMfa(emp: any) {
    const nextStatus = !emp.isMfaEnabled;
    this.employeeService.toggleMfa(emp.empID, nextStatus).subscribe({
      next: (res: any) => {
        emp.isMfaEnabled = nextStatus;
        this.toastService.success(res.message || `MFA updated for ${emp.empName}.`);
      },
      error: (err) => {
        console.error(err);
        this.toastService.error('Failed to update MFA status.');
        // Revert UI toggle state
        this.loadEmployees();
      }
    });
  }

  // Selection Checkbox handlers
  isAllSelected() {
    const currentList = this.filteredAndPagedEmployees;
    if (currentList.length === 0) return false;
    return currentList.every(emp => this.selectedEmpIds.has(emp.empID));
  }

  toggleSelectAll() {
    const currentList = this.filteredAndPagedEmployees;
    if (this.isAllSelected()) {
      currentList.forEach(emp => this.selectedEmpIds.delete(emp.empID));
    } else {
      currentList.forEach(emp => this.selectedEmpIds.add(emp.empID));
    }
  }

  toggleSelect(empId: string) {
    if (this.selectedEmpIds.has(empId)) {
      this.selectedEmpIds.delete(empId);
    } else {
      this.selectedEmpIds.add(empId);
    }
  }

  // Bulk Actions
  bulkToggleMfaStatus(enable: boolean) {
    if (this.selectedEmpIds.size === 0) {
      this.toastService.warning('No employees selected.');
      return;
    }

    const empIdsArray = Array.from(this.selectedEmpIds);
    this.isLoading = true;

    this.employeeService.bulkToggleMfa(empIdsArray, enable).subscribe({
      next: (res: any) => {
        this.toastService.success(res.message || `Bulk MFA update completed successfully.`);
        this.selectedEmpIds.clear();
        this.loadEmployees();
      },
      error: (err) => {
        console.error(err);
        this.toastService.error('Failed to perform bulk MFA update.');
        this.isLoading = false;
      }
    });
  }

  getInitials(name: string): string {
    if (!name) return 'EE';
    const parts = name.trim().split(/\s+/);
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return parts[0].slice(0, 2).toUpperCase();
  }
}
