import { Routes } from '@angular/router';
import { contentAuthorGuard, practiceGuard } from './guards/auth.guard';
import { LoginPage } from './pages/login.page';
import { GradesPage } from './pages/grades.page';
import { RulesPage } from './pages/rules.page';
import { SentencesPage } from './pages/sentences.page';
import { ImportPage } from './pages/import.page';
import { AdminTestsPage } from './pages/admin-tests.page';
import { PracticeSetupPage } from './pages/practice-setup.page';
import { PracticeSessionPage } from './pages/practice-session.page';
import { TestTakePage } from './pages/test-take.page';

export const routes: Routes = [
  { path: 'login', component: LoginPage },
  { path: 'practice', component: PracticeSetupPage, canActivate: [practiceGuard] },
  { path: 'practice/:id', component: PracticeSessionPage, canActivate: [practiceGuard] },
  { path: 'tests/:id/take', component: TestTakePage, canActivate: [practiceGuard] },
  { path: 'admin/grades', component: GradesPage, canActivate: [contentAuthorGuard] },
  { path: 'admin/rules', component: RulesPage, canActivate: [contentAuthorGuard] },
  { path: 'admin/sentences', component: SentencesPage, canActivate: [contentAuthorGuard] },
  { path: 'admin/tests', component: AdminTestsPage, canActivate: [contentAuthorGuard] },
  { path: 'admin/import', component: ImportPage, canActivate: [contentAuthorGuard] },
  { path: '', pathMatch: 'full', redirectTo: 'practice' },
  { path: '**', redirectTo: 'practice' }
];
