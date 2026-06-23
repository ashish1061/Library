import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { jwtDecode } from 'jwt-decode';
import { SessionService } from './core/services/session.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrls: ['./app.css'],
  standalone: false
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'JSL Library';
  sidebarCollapsed = false;
  mobileSidebarOpen = false;
  
  librarianMenuExpanded = true;
  adminMenuExpanded = false;
  
  isWarningActive = false;
  countdownSeconds = 120;
  countdownFormatted = '02:00';
  
  private subs: Subscription = new Subscription();

  constructor(
    private router: Router,
    private sessionService: SessionService
  ) {}

  get isAuthPage(): boolean {
    return this.router.url.includes('/auth/login') || this.router.url === '/';
  }

  get isAdmin(): boolean {
    const token = localStorage.getItem('accessToken');
    if (!token) return false;
    try {
      const decoded: any = jwtDecode(token);
      const role = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || decoded.role;
      return role === 'Admin';
    } catch {
      return false;
    }
  }

  get userName(): string {
    const token = localStorage.getItem('accessToken');
    if (!token) return 'User';
    try {
      const decoded: any = jwtDecode(token);
      return decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || decoded.unique_name || 'User';
    } catch {
      return 'User';
    }
  }

  get userInitial(): string {
    return this.userName.charAt(0).toUpperCase();
  }

  toggleSidebar() {
    if (window.innerWidth <= 768) {
      this.mobileSidebarOpen = !this.mobileSidebarOpen;
    } else {
      this.sidebarCollapsed = !this.sidebarCollapsed;
    }
  }

  ngOnInit() {
    // Start session monitoring on application load
    this.sessionService.startTracking();

    // Subscribe to session expiration warnings
    this.subs.add(
      this.sessionService.isWarningActive$.subscribe(active => {
        this.isWarningActive = active;
      })
    );

    // Subscribe to warning countdown timer
    this.subs.add(
      this.sessionService.countdown$.subscribe(seconds => {
        this.countdownSeconds = seconds;
        this.countdownFormatted = this.formatCountdown(seconds);
      })
    );
  }

  ngOnDestroy() {
    this.subs.unsubscribe();
    this.sessionService.stopTracking();
  }

  extendSession(): void {
    this.sessionService.extendSession();
  }

  logout(): void {
    this.sessionService.logout();
  }

  private formatCountdown(totalSeconds: number): string {
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    const padding = (num: number) => num < 10 ? '0' + num : num;
    return `${padding(minutes)}:${padding(seconds)}`;
  }
}

