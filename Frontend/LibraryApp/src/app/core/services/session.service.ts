import { Injectable, NgZone } from '@angular/core';
import { Store } from '@ngxs/store';
import { Router, NavigationEnd } from '@angular/router';
import { BehaviorSubject, Observable } from 'rxjs';
import { filter } from 'rxjs/operators';
import { Logout } from '../../store/auth.state';

@Injectable({
  providedIn: 'root'
})
export class SessionService {
  private readonly SESSION_TIMEOUT = 20 * 60 * 1000; // 20 minutes in ms
  private readonly WARNING_TIMEOUT = 18 * 60 * 1000; // 18 minutes in ms
  private readonly COUNTDOWN_TIME = 2 * 60; // 2 minutes in seconds

  private checkInterval: any;
  private countdownInterval: any;

  private isWarningActiveSubject = new BehaviorSubject<boolean>(false);
  isWarningActive$: Observable<boolean> = this.isWarningActiveSubject.asObservable();

  private countdownSubject = new BehaviorSubject<number>(this.COUNTDOWN_TIME);
  countdown$: Observable<number> = this.countdownSubject.asObservable();

  constructor(
    private store: Store,
    private router: Router,
    private ngZone: NgZone
  ) {
    // Listen to routing changes to ensure we track properly on navigate
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.checkSessionStatusOnNavigation();
    });
  }

  startTracking() {
    this.stopTracking();

    if (!this.isAuthenticated()) {
      return;
    }
    
    // Set initial activity time if not set
    if (!localStorage.getItem('session_last_activity')) {
      localStorage.setItem('session_last_activity', Date.now().toString());
    }

    // Set up listeners for user activity
    this.ngZone.runOutsideAngular(() => {
      const activityEvents = ['mousemove', 'mousedown', 'keypress', 'scroll', 'click', 'touchstart'];
      activityEvents.forEach(event => {
        window.addEventListener(event, this.resetActivity, true);
      });

      // Synchronize across multiple tabs
      window.addEventListener('storage', this.handleStorageChange, true);
    });

    // Periodically check session status (every 2 seconds keeps CPU usage zero while maintaining precision)
    this.checkInterval = setInterval(() => {
      this.ngZone.run(() => {
        this.checkSession();
      });
    }, 2000);
  }

  stopTracking() {
    if (this.checkInterval) {
      clearInterval(this.checkInterval);
      this.checkInterval = null;
    }
    this.stopCountdown();

    const activityEvents = ['mousemove', 'mousedown', 'keypress', 'scroll', 'click', 'touchstart'];
    activityEvents.forEach(event => {
      window.removeEventListener(event, this.resetActivity, true);
    });
    window.removeEventListener('storage', this.handleStorageChange, true);

    this.isWarningActiveSubject.next(false);
  }

  private resetActivity = () => {
    // Only reset activity if warning is not active and user is authenticated
    if (!this.isWarningActiveSubject.value && this.isAuthenticated()) {
      localStorage.setItem('session_last_activity', Date.now().toString());
    }
  }

  private handleStorageChange = (event: StorageEvent) => {
    if (event.key === 'session_last_activity' && event.newValue) {
      this.ngZone.run(() => {
        // If other tab updated activity, reset our warning popup
        if (this.isWarningActiveSubject.value) {
          this.isWarningActiveSubject.next(false);
          this.stopCountdown();
        }
      });
    }
  }

  private checkSessionStatusOnNavigation() {
    const isAuthPage = this.router.url.includes('/auth');
    if (isAuthPage) {
      this.stopTracking();
    } else if (this.isAuthenticated() && !this.checkInterval) {
      this.startTracking();
    }
  }

  private checkSession() {
    if (!this.isAuthenticated()) {
      this.stopTracking();
      return;
    }

    const lastActivity = parseInt(localStorage.getItem('session_last_activity') || '0', 10);
    const now = Date.now();
    const elapsed = now - lastActivity;

    if (elapsed >= this.WARNING_TIMEOUT) {
      // Session is in warning window
      if (!this.isWarningActiveSubject.value) {
        this.triggerWarning(lastActivity);
      }
    } else {
      // Normal active state
      if (this.isWarningActiveSubject.value) {
        this.isWarningActiveSubject.next(false);
        this.stopCountdown();
      }
    }
  }

  private triggerWarning(lastActivity: number) {
    this.isWarningActiveSubject.next(true);
    
    // Calculate initial countdown based on elapsed time in case page was inactive/backgrounded
    const elapsedSeconds = Math.floor((Date.now() - lastActivity - this.WARNING_TIMEOUT) / 1000);
    const initialCountdown = Math.max(0, this.COUNTDOWN_TIME - elapsedSeconds);
    this.countdownSubject.next(initialCountdown);

    this.startCountdown();
  }

  private startCountdown() {
    this.stopCountdown();
    this.countdownInterval = setInterval(() => {
      const current = this.countdownSubject.value;
      if (current <= 1) {
        this.countdownSubject.next(0);
        this.logout();
      } else {
        this.countdownSubject.next(current - 1);
      }
    }, 1000);
  }

  private stopCountdown() {
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
      this.countdownInterval = null;
    }
    this.countdownSubject.next(this.COUNTDOWN_TIME);
  }

  extendSession() {
    localStorage.setItem('session_last_activity', Date.now().toString());
    this.isWarningActiveSubject.next(false);
    this.stopCountdown();
  }

  logout() {
    this.stopTracking();
    localStorage.removeItem('session_last_activity');
    this.store.dispatch(new Logout()).subscribe(() => {
      this.router.navigate(['/auth/login']);
    });
  }

  private isAuthenticated(): boolean {
    return !!localStorage.getItem('accessToken');
  }
}
