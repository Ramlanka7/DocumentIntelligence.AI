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

const ACCEPTED_TYPES = [
  'application/pdf',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  'text/plain',
];
const ACCEPTED_EXTENSIONS = ['.pdf', '.docx', '.txt'];
const MAX_FILE_SIZE_BYTES = 50 * 1024 * 1024; // 50 MB
const MIN_FILES = 2;
const MAX_FILES = 4;

@Component({
  selector: 'app-document-upload',
  standalone: true,
  imports: [MatButtonModule, MatIconModule],
  templateUrl: './document-upload.html',
  styleUrl: './document-upload.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DocumentUploadComponent {
  readonly files = input<File[]>([]);
  readonly filesChange = output<File[]>();
  readonly validationError = output<string>();

  protected readonly fileInput = viewChild.required<ElementRef<HTMLInputElement>>('fileInput');

  protected readonly isDragging = signal(false);
  protected readonly localError = signal<string | null>(null);

  protected readonly minFiles = MIN_FILES;
  protected readonly maxFiles = MAX_FILES;
  protected readonly acceptedExtensions = ACCEPTED_EXTENSIONS.join(', ');

  protected openFilePicker(): void {
    this.fileInput().nativeElement.click();
  }

  protected onFileInputChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files) {
      this.processFiles(Array.from(input.files));
      // Reset so the same file can be re-selected after removal
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
    if (file.type === 'application/pdf') return 'picture_as_pdf';
    if (file.type.includes('word') || file.name.endsWith('.docx')) return 'description';
    return 'text_snippet';
  }

  private processFiles(incoming: File[]): void {
    this.localError.set(null);

    const invalidType = incoming.find((f) => !this.isValidType(f));
    if (invalidType) {
      const msg = `"${invalidType.name}" is not a supported type. Only PDF, DOCX, and TXT files are accepted.`;
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
      const msg = `You can upload a maximum of ${MAX_FILES} documents for comparison.`;
      this.localError.set(msg);
      this.validationError.emit(msg);
      return;
    }

    this.filesChange.emit(merged);
  }

  private isValidType(file: File): boolean {
    if (ACCEPTED_TYPES.includes(file.type)) return true;
    const ext = file.name.split('.').pop()?.toLowerCase() ?? '';
    return ACCEPTED_EXTENSIONS.some((e) => e === `.${ext}`);
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
