import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { JsonPipe } from '@angular/common';
import { ContentService, ImportBatchDto } from '../services/content.service';

@Component({
  standalone: true,
  selector: 'app-import-page',
  imports: [FormsModule, JsonPipe],
  template: `
    <div class="head">
      <h2>Bulk import</h2>
    </div>
    <p class="lede">Primary way to add grades, rules, and sentences. Download a template, fill rows, upload to validate, then commit. Rows land as Draft unless Status=Approved.</p>

    <section class="form">
      <label>Entity
        <select [(ngModel)]="entityKind">
          <option value="grades">Grades</option>
          <option value="rules">Rules</option>
          <option value="sentences">Sentences</option>
        </select>
      </label>
      <div class="actions">
        <button type="button" class="ghost" (click)="download('csv')">Download CSV template</button>
        <button type="button" class="ghost" (click)="download('xlsx')">Download Excel template</button>
      </div>
      <label>File
        <input type="file" accept=".csv,.xlsx,.xlsm" (change)="onFile($event)" />
      </label>
      <button type="button" (click)="upload()" [disabled]="!file || busy()">Upload &amp; validate</button>
    </section>

    @if (error()) { <p class="error">{{ error() }}</p> }
    @if (message()) { <p class="hint">{{ message() }}</p> }

    @if (batch(); as b) {
      <section class="form">
        <h3>Batch #{{ b.id }} — {{ statusLabel(b.status) }}</h3>
        <p>{{ b.fileName }} · rows {{ b.rowCount }} · ok {{ b.successCount }} · errors {{ b.errorCount }}</p>
        @if (b.errors.length) {
          <table>
            <thead><tr><th>Row</th><th>Field</th><th>Message</th></tr></thead>
            <tbody>
              @for (e of b.errors; track $index) {
                <tr><td>{{ e.rowNumber }}</td><td>{{ e.field }}</td><td>{{ e.message }}</td></tr>
              }
            </tbody>
          </table>
        }
        @if (b.previewRows.length) {
          <details>
            <summary>Preview rows ({{ b.previewRows.length }})</summary>
            <pre class="ping">{{ b.previewRows | json }}</pre>
          </details>
        }
        @if (b.status === 1) {
          <button type="button" (click)="commit()" [disabled]="busy()">Confirm commit</button>
        }
      </section>
    }
  `,
  styleUrl: './admin-shared.scss'
})
export class ImportPage {
  entityKind = 'sentences';
  file: File | null = null;
  readonly batch = signal<ImportBatchDto | null>(null);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);

  constructor(private readonly api: ContentService) {}

  onFile(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    this.file = input.files?.[0] ?? null;
  }

  download(format: 'csv' | 'xlsx'): void {
    this.api.downloadTemplate(this.entityKind, format).subscribe({
      next: (blob) => {
        const a = document.createElement('a');
        a.href = URL.createObjectURL(blob);
        a.download = `enur-${this.entityKind}-template.${format}`;
        a.click();
        URL.revokeObjectURL(a.href);
      },
      error: () => this.error.set('Template download failed.')
    });
  }

  upload(): void {
    if (!this.file) return;
    this.busy.set(true);
    this.error.set(null);
    this.message.set(null);
    this.api.uploadImport(this.file, this.entityKind).subscribe({
      next: (b) => {
        this.busy.set(false);
        this.batch.set(b);
        this.message.set(b.status === 1 ? 'Validated — review and commit.' : 'Validation failed — fix rows and re-upload.');
      },
      error: (e) => {
        this.busy.set(false);
        this.error.set(e?.error?.error ?? 'Upload failed.');
      }
    });
  }

  commit(): void {
    const b = this.batch();
    if (!b) return;
    this.busy.set(true);
    this.api.commitImport(b.id).subscribe({
      next: (res) => {
        this.busy.set(false);
        this.message.set(`Committed ${res.successCount} row(s).`);
        this.api.uploadImport; // no-op keep ref
        // refresh batch
        this.batch.update((cur) => cur ? { ...cur, status: res.status, successCount: res.successCount, committedAtUtc: new Date().toISOString() } : cur);
      },
      error: (e) => {
        this.busy.set(false);
        this.error.set(e?.error?.error ?? 'Commit failed.');
      }
    });
  }

  statusLabel(s: number): string {
    return ['Pending', 'Validated', 'Committed', 'Failed'][s] ?? String(s);
  }
}
