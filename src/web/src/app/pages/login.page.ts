import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  standalone: true,
  selector: 'app-login-page',
  imports: [FormsModule],
  template: `
    <section class="panel">
      <h2>Sign in</h2>
      <p class="hint">Student practice: student&#64;enur.local · Admin: admin&#64;enur.local</p>
      <label>Email<input type="email" [(ngModel)]="email" autocomplete="username" /></label>
      <label>Password<input type="password" [(ngModel)]="password" autocomplete="current-password" /></label>
      <button type="button" (click)="submit()" [disabled]="busy()">Sign in</button>
      @if (error()) {
        <p class="error" role="alert">{{ error() }}</p>
      }
    </section>
  `,
  styles: [`
    .panel { display: grid; gap: .85rem; max-width: 22rem; }
    label { display: grid; gap: .35rem; font-size: .85rem; color: var(--muted); }
    input { font: inherit; padding: .65rem .75rem; border: 1px solid var(--line); border-radius: .4rem; background: rgba(255,255,255,.72); }
    button { font: inherit; font-weight: 600; color: #fff; background: var(--accent); border: 0; border-radius: .4rem; padding: .7rem 1rem; cursor: pointer; }
    .hint { margin: 0; color: var(--muted); font-size: .9rem; }
    .error { color: #8b1e1e; }
  `]
})
export class LoginPage {
  email = 'student@enur.local';
  password = 'ChangeMe!Student123';
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  constructor(private readonly auth: AuthService, private readonly router: Router) {}

  submit(): void {
    this.busy.set(true);
    this.error.set(null);
    this.auth.login(this.email.trim(), this.password).subscribe({
      next: () => {
        this.busy.set(false);
        this.router.navigateByUrl('/practice');
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err?.error?.error ?? 'Login failed.');
      }
    });
  }
}
