import { Routes } from '@angular/router';
import { authGuard, contentAuthorGuard } from './guards/auth.guard';
import { LoginPage } from './pages/login.page';
import { GradesPage } from './pages/grades.page';
import { RulesPage } from './pages/rules.page';
import { SentencesPage } from './pages/sentences.page';
import { ImportPage } from './pages/import.page';

export const routes: Routes = [
  { path: 'login', component: LoginPage },
  { path: 'admin/grades', component: GradesPage, canActivate: [contentAuthorGuard] },
  { path: 'admin/rules', component: RulesPage, canActivate: [contentAuthorGuard] },
  { path: 'admin/sentences', component: SentencesPage, canActivate: [contentAuthorGuard] },
  { path: 'admin/import', component: ImportPage, canActivate: [contentAuthorGuard] },
  { path: '', pathMatch: 'full', redirectTo: 'admin/sentences' },
  { path: '**', redirectTo: 'admin/sentences' }
];
