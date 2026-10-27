import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface UserInfo {
  id: string;
  email: string;
  displayName: string | null;
  roles: string[];
}

export interface LoginResponse {
  accessToken: string;
  expiresAtUtc: string;
  user: UserInfo;
}

export interface PingResponse {
  message: string;
  utcNow: string;
  userId: string;
  roles: string[];
}

const TOKEN_KEY = 'enur_access_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly user = signal<UserInfo | null>(null);
  readonly token = signal<string | null>(this.readToken());

  constructor(private readonly http: HttpClient) {}

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${environment.apiBaseUrl}/api/auth/login`, { email, password })
      .pipe(
        tap((res) => {
          localStorage.setItem(TOKEN_KEY, res.accessToken);
          this.token.set(res.accessToken);
          this.user.set(res.user);
        })
      );
  }

  me(): Observable<UserInfo> {
    return this.http.get<UserInfo>(`${environment.apiBaseUrl}/api/auth/me`).pipe(
      tap((u) => this.user.set(u))
    );
  }

  ping(): Observable<PingResponse> {
    return this.http.get<PingResponse>(`${environment.apiBaseUrl}/api/ping`);
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this.token.set(null);
    this.user.set(null);
  }

  private readToken(): string | null {
    try {
      return localStorage.getItem(TOKEN_KEY);
    } catch {
      return null;
    }
  }
}
