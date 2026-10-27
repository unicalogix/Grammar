import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ContentService, RuleDto } from '../services/content.service';

@Component({
  standalone: true,
  selector: 'app-rules-page',
  imports: [FormsModule],
  template: `
    <div class="head">
      <h2>Rules / tenses</h2>
      <button type="button" (click)="startNew()">New rule</button>
    </div>

    @if (editing()) {
      <section class="form">
        <h3>{{ form.id ? 'Edit' : 'New' }} rule</h3>
        <label>Code<input [(ngModel)]="form.code" placeholder="PAST_SIMPLE" /></label>
        <label>Name (EN)<input [(ngModel)]="form.nameEn" /></label>
        <label>Name (UR)<input [(ngModel)]="form.nameUr" dir="rtl" /></label>
        <label>Explanation (EN)<textarea [(ngModel)]="form.explanationEn" rows="2"></textarea></label>
        <label>Explanation (UR)<textarea [(ngModel)]="form.explanationUr" rows="2" dir="rtl"></textarea></label>
        <label class="check"><input type="checkbox" [(ngModel)]="form.isActive" /> Active</label>
        <div class="actions">
          <button type="button" (click)="save()" [disabled]="busy()">Save</button>
          <button type="button" class="ghost" (click)="editing.set(false)">Cancel</button>
        </div>
      </section>
    }

    @if (error()) { <p class="error">{{ error() }}</p> }

    <table>
      <thead><tr><th>Code</th><th>Name</th><th>Active</th><th></th></tr></thead>
      <tbody>
        @for (r of rules(); track r.id) {
          <tr>
            <td>{{ r.code || '—' }}</td>
            <td>{{ r.nameEn }} <span class="muted" dir="rtl">{{ r.nameUr }}</span></td>
            <td>{{ r.isActive ? 'yes' : 'no' }}</td>
            <td><button type="button" class="ghost" (click)="edit(r)">Edit</button></td>
          </tr>
        }
      </tbody>
    </table>
  `,
  styleUrl: './admin-shared.scss'
})
export class RulesPage implements OnInit {
  readonly rules = signal<RuleDto[]>([]);
  readonly editing = signal(false);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  form = this.empty();

  constructor(private readonly api: ContentService) {}

  ngOnInit(): void { this.reload(); }

  startNew(): void {
    this.form = this.empty();
    this.editing.set(true);
  }

  edit(r: RuleDto): void {
    this.form = {
      id: r.id,
      code: r.code ?? '',
      nameEn: r.nameEn,
      nameUr: r.nameUr ?? '',
      explanationEn: r.explanationEn ?? '',
      explanationUr: r.explanationUr ?? '',
      isActive: r.isActive
    };
    this.editing.set(true);
  }

  save(): void {
    this.busy.set(true);
    this.error.set(null);
    this.api.saveRule({
      nameEn: this.form.nameEn,
      nameUr: this.form.nameUr || null,
      explanationEn: this.form.explanationEn || null,
      explanationUr: this.form.explanationUr || null,
      code: this.form.code || null,
      isActive: this.form.isActive
    }, this.form.id).subscribe({
      next: () => {
        this.busy.set(false);
        this.editing.set(false);
        this.reload();
      },
      error: (e) => {
        this.busy.set(false);
        this.error.set(e?.error?.error ?? 'Save failed.');
      }
    });
  }

  private reload(): void {
    this.api.listRules().subscribe({
      next: (r) => this.rules.set(r),
      error: (e) => this.error.set(e?.error?.error ?? 'Failed to load rules.')
    });
  }

  private empty() {
    return {
      id: undefined as number | undefined,
      code: '',
      nameEn: '',
      nameUr: '',
      explanationEn: '',
      explanationUr: '',
      isActive: true
    };
  }
}
