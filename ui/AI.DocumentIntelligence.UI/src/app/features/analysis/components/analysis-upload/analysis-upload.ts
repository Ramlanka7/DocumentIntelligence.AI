import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import { AnalysisFile } from '../../models/analysis.models';

const ACCEPTED_MIME_TYPES = [
  'application/pdf',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  'text/plain',
  'text/csv',
  'application/csv',
  'application/vnd.openxmlformats-officedocument.presentationml.presentation',
];

const ACCEPTED_EXTENSIONS = ['.pdf', '.docx', '.txt', '.csv', '.pptx'];
const MAX_FILE_SIZE_BYTES = 50 * 1024 * 1024; // 50 MB per file
const MAX_FILES = 4;

@Component({
  selector: 'app-analysis-upload',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatProgressBarModule],
  templateUrl: './analysis-upload.html',
  styleUrl: './analysis-upload.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AnalysisUploadComponent {
  /** Current file list managed by the parent smart component. */
  readonly files = input<File[]>([]);
  /** Per-file status provided by the service once uploading begins. */
  readonly analysisFiles = input<AnalysisFile[]>([]);

  readonly filesChange = output<File[]>();
  readonly validationError = output<string>();

  protected readonly fileInput = viewChild.required<ElementRef<HTMLInputElement>>('fileInput');
  protected readonly isDragging = signal(false);
  protected readonly localError = signal<string | null>(null);

  protected readonly maxFiles = MAX_FILES;
  protected readonly acceptedExtensions = ACCEPTED_EXTENSIONS.join(', ');
  protected readonly acceptAttr = ACCEPTED_EXTENSIONS.join(',');

  protected openFilePicker(): void {
    this.fileInput().nativeElement.click();
  }

  protected onFileInputChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files) {
      this.processFiles(Array.from(input.files));
      input.value = '';
    }
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(true);
  }

  protected onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(false);
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(false);

    const droppedFiles = event.dataTransfer?.files;
    if (droppedFiles) {
      this.processFiles(Array.from(droppedFiles));
    }
  }

  protected removeFile(index: number): void {
    const updated = this.files().filter((_, i) => i !== index);
    this.filesChange.emit(updated);
    this.localError.set(null);
  }

  protected formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  protected getFileIcon(file: File): string {
    if (file.type === 'application/pdf' || file.name.endsWith('.pdf')) return 'picture_as_pdf';
    if (file.type.includes('word') || file.name.endsWith('.docx')) return 'description';
    if (file.type.includes('presentation') || file.name.endsWith('.pptx')) return 'slideshow';
    if (file.type.includes('csv') || file.name.endsWith('.csv')) return 'table_chart';
    return 'text_snippet';
  }

  protected getFileStatus(index: number): AnalysisFile | null {
    return this.analysisFiles()[index] ?? null;
  }

  protected getStatusIcon(status: AnalysisFile['status']): string {
    switch (status) {
      case 'uploading': return 'cloud_upload';
      case 'success': return 'check_circle';
      case 'error': return 'error_outline';
      default: return 'schedule';
    }
  }

  private processFiles(incoming: File[]): void {
    this.localError.set(null);

    const invalidType = incoming.find((f) => !this.isValidType(f));
    if (invalidType) {
      const msg = `"${invalidType.name}" is not a supported type. Accepted: PDF, DOCX, TXT, CSV, PPTX.`;
      this.localError.set(msg);
      this.validationError.emit(msg);
      return;
    }

    const oversized = incoming.find((f) => f.size > MAX_FILE_SIZE_BYTES);
    if (oversized) {
      const msg = `"${oversized.name}" exceeds the 50 MB limit.`;
      this.localError.set(msg);
      this.validationError.emit(msg);
      return;
    }

    const merged = this.deduplicateFiles([...this.files(), ...incoming]);

    if (merged.length > MAX_FILES) {
      const msg = `You can upload a maximum of ${MAX_FILES} documents for analysis.`;
      this.localError.set(msg);
      this.validationError.emit(msg);
      return;
    }

    this.filesChange.emit(merged);
  }

  private isValidType(file: File): boolean {
    if (ACCEPTED_MIME_TYPES.includes(file.type)) return true;
    const ext = `.${file.name.split('.').pop()?.toLowerCase() ?? ''}`;
    return ACCEPTED_EXTENSIONS.includes(ext);
  }

  private deduplicateFiles(files: File[]): File[] {
    const seen = new Set<string>();
    return files.filter((f) => {
      const key = `${f.name}-${f.size}`;
      if (seen.has(key)) return false;
      seen.add(key);
      return true;
    });
  }
}
