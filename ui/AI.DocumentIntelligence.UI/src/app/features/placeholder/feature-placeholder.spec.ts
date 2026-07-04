import { TestBed } from '@angular/core/testing';

import { FeaturePlaceholder } from './feature-placeholder';

describe('FeaturePlaceholder', () => {
  it('should create and render the provided title', () => {
    const fixture = TestBed.createComponent(FeaturePlaceholder);
    fixture.componentRef.setInput('icon', 'insights');
    fixture.componentRef.setInput('title', 'Analysis Mode');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.placeholder__title')?.textContent?.trim()).toBe('Analysis Mode');
  });
});
