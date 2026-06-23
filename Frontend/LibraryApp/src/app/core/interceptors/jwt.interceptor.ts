import { Injectable } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { catchError, filter, take, switchMap } from 'rxjs/operators';
import { Store } from '@ngxs/store';
import { AuthState, Logout, SetTokens } from '../../store/auth.state';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable()
export class JwtInterceptor implements HttpInterceptor {
    private isRefreshing = false;
    private refreshTokenSubject: BehaviorSubject<any> = new BehaviorSubject<any>(null);

    constructor(private store: Store, private router: Router, private authService: AuthService) { }

    intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
        const token = this.store.selectSnapshot(AuthState.token);
        let authReq = request;
        if (token && !request.url.includes('/auth/login') && !request.url.includes('/auth/register')) {
            authReq = this.addToken(request, token);
        }

        return next.handle(authReq).pipe(catchError(error => {
            if (error instanceof HttpErrorResponse && error.status === 401) {
                // Do not intercept 401s from the login endpoint (it means invalid credentials, not an expired session)
                if (request.url.includes('/auth/login')) {
                    return throwError(() => error);
                }
                return this.handle401Error(authReq, next);
            } else {
                return throwError(() => error);
            }
        }));
    }

    private addToken(request: HttpRequest<any>, token: string) {
        return request.clone({
            setHeaders: {
                Authorization: `Bearer ${token}`
            }
        });
    }

    private handle401Error(request: HttpRequest<any>, next: HttpHandler) {
        if (!this.isRefreshing) {
            this.isRefreshing = true;
            this.refreshTokenSubject.next(null);

            const accessToken = localStorage.getItem('accessToken') || '';
            const refreshToken = localStorage.getItem('refreshToken') || '';

            if (accessToken && refreshToken) {
                return this.authService.refreshToken(accessToken, refreshToken).pipe(
                    switchMap((res: any) => {
                        this.isRefreshing = false;
                        this.store.dispatch(new SetTokens({ accessToken: res.accessToken, refreshToken: res.refreshToken }));
                        this.refreshTokenSubject.next(res.accessToken);
                        return next.handle(this.addToken(request, res.accessToken));
                    }),
                    catchError((err) => {
                        this.isRefreshing = false;
                        this.store.dispatch(new Logout());
                        this.router.navigate(['/auth/login']);
                        return throwError(() => err);
                    })
                );
            } else {
                this.isRefreshing = false;
                this.store.dispatch(new Logout());
                this.router.navigate(['/auth/login']);
                return throwError(() => new Error('Session expired'));
            }
        } else {
            return this.refreshTokenSubject.pipe(
                filter(token => token != null),
                take(1),
                switchMap(jwt => {
                    return next.handle(this.addToken(request, jwt));
                })
            );
        }
    }
}
