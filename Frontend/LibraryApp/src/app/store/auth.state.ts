import { State, Action, StateContext, Selector } from '@ngxs/store';
import { Injectable } from '@angular/core';

export class SetTokens {
  static readonly type = '[Auth] Set Tokens';
  constructor(public payload: { accessToken: string; refreshToken: string }) {}
}

export class Logout {
  static readonly type = '[Auth] Logout';
}

export interface AuthStateModel {
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
}

@State<AuthStateModel>({
  name: 'auth',
  defaults: {
    accessToken: null,
    refreshToken: null,
    isAuthenticated: false
  }
})
@Injectable()
export class AuthState {
  
  @Selector()
  static isAuthenticated(state: AuthStateModel): boolean {
    return state.isAuthenticated;
  }

  @Selector()
  static token(state: AuthStateModel): string | null {
    return state.accessToken;
  }

  @Action(SetTokens)
  setTokens(ctx: StateContext<AuthStateModel>, action: SetTokens) {
    ctx.patchState({
      accessToken: action.payload.accessToken,
      refreshToken: action.payload.refreshToken,
      isAuthenticated: true
    });
    // Store in localStorage for persistence
    localStorage.setItem('accessToken', action.payload.accessToken);
    localStorage.setItem('refreshToken', action.payload.refreshToken);
  }

  @Action(Logout)
  logout(ctx: StateContext<AuthStateModel>) {
    ctx.patchState({
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false
    });
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
  }
}
