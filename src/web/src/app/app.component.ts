import { Component, OnInit, computed } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  readonly isAuthor = computed(() => {
    const roles = this.auth.user()?.roles ?? [];
    return roles.includes('Admin') || roles.includes('Teacher');
  });

  constructor(readonly auth: AuthService, private readonly router: Router) {}

  ngOnInit(): void {
    if (this.auth.token() && !this.auth.user()) {
      this.auth.me().subscribe({ error: () => this.auth.logout() });
    }
  }

  logout(): void {
    this.auth.logout();
    sessionStorage.removeItem('enur_practice_session');
    this.router.navigateByUrl('/login');
  }
}
