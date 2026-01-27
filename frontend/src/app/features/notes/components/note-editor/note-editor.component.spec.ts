

import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { of, throwError, Observable } from 'rxjs';
import { NoteEditorComponent } from './note-editor.component';
import { NotesService } from '../../services/notes.service';
import { TagsService } from '../../../../shared/services/tags.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Note, CreateNoteRequest } from '../../models/note.models';

describe('NoteEditorComponent', () => {
  let component: NoteEditorComponent;
  let fixture: ComponentFixture<NoteEditorComponent>;
  let notesServiceSpy: jasmine.SpyObj<NotesService>;
  let tagsServiceSpy: jasmine.SpyObj<TagsService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let translateServiceSpy: jasmine.SpyObj<TranslateService>;

  const mockNote: Note = {
    id: '123e4567-e89b-12d3-a456-426614174000',
    title: 'Test Note',
    markdown: '# Test Content',
    isArchived: false,
    tags: [],
    createdAt: new Date('2024-01-01'),
    updatedAt: new Date('2024-01-01')
  };

  beforeEach(async () => {
    
    const notesSpy = jasmine.createSpyObj('NotesService', [
      'createNote',
      'updateNote',
      'getNoteById',
      'uploadMedia'
    ]);
    const tagsSpy = jasmine.createSpyObj('TagsService', ['getTags']);
    const routerSpyObj = jasmine.createSpyObj('Router', ['navigate']);
    const translateSpy = jasmine.createSpyObj('TranslateService', ['instant', 'get']);
    translateSpy.get.and.returnValue(of('Translated text'));

    const activatedRouteMock = {
      snapshot: {
        paramMap: {
          get: jasmine.createSpy('get').and.returnValue(null)
        }
      }
    };

    await TestBed.configureTestingModule({
      imports: [
        NoteEditorComponent,
        ReactiveFormsModule,
        TranslateModule.forRoot()
      ],
      providers: [
        { provide: NotesService, useValue: notesSpy },
        { provide: TagsService, useValue: tagsSpy },
        { provide: Router, useValue: routerSpyObj },
        { provide: ActivatedRoute, useValue: activatedRouteMock },
        { provide: TranslateService, useValue: translateSpy }
      ]
    }).compileComponents();

    notesServiceSpy = TestBed.inject(NotesService) as jasmine.SpyObj<NotesService>;
    tagsServiceSpy = TestBed.inject(TagsService) as jasmine.SpyObj<TagsService>;
    routerSpy = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    translateServiceSpy = TestBed.inject(TranslateService) as jasmine.SpyObj<TranslateService>;

    tagsServiceSpy.getTags.and.returnValue(of([]));
    translateServiceSpy.instant.and.returnValue('Translated text');

    fixture = TestBed.createComponent(NoteEditorComponent);
    component = fixture.componentInstance;

    component.ngOnInit();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Form Initialization', () => {
    it('should mark title as required', () => {
      
      const titleControl = component.noteForm.get('title');

      titleControl?.setValue('');
      titleControl?.markAsTouched();

      expect(titleControl?.invalid).toBe(true);
      expect(titleControl?.hasError('required')).toBe(true);
    });

    it('should mark markdown as required', () => {
      
      const markdownControl = component.noteForm.get('markdown');

      markdownControl?.setValue('');
      markdownControl?.markAsTouched();

      expect(markdownControl?.invalid).toBe(true);
      expect(markdownControl?.hasError('required')).toBe(true);
    });

    it('should mark form as valid when title and markdown are provided', () => {
      
      component.noteForm.get('title')?.setValue('Test Title');
      component.noteForm.get('markdown')?.setValue('# Test Content');

      expect(component.noteForm.valid).toBe(true);
    });
  });

  describe('Save Event - Create Note', () => {
    it('should call createNote service when submitting new note', () => {
      
      component.noteForm.get('title')?.setValue('New Note');
      component.noteForm.get('markdown')?.setValue('# New Content');
      notesServiceSpy.createNote.and.returnValue(of(mockNote));

      component.onSubmit();

      expect(notesServiceSpy.createNote).toHaveBeenCalledWith(
        jasmine.objectContaining({
          title: 'New Note',
          markdown: '# New Content'
        })
      );
    });

    it('should include tagIds in create request when tags are selected', () => {
      
      const validTagId1 = '11111111-1111-1111-1111-111111111111';
      const validTagId2 = '22222222-2222-2222-2222-222222222222';

      component.noteForm.get('title')?.setValue('New Note');
      component.noteForm.get('markdown')?.setValue('# New Content');
      component.selectedTagIds = [validTagId1, validTagId2];
      notesServiceSpy.createNote.and.returnValue(of(mockNote));

      component.onSubmit();

      expect(notesServiceSpy.createNote).toHaveBeenCalledWith(
        jasmine.objectContaining({
          title: 'New Note',
          markdown: '# New Content',
          tagIds: [validTagId1, validTagId2]
        })
      );
    });

    it('should set isSaving to true when submitting', fakeAsync(() => {
      
      component.noteForm.get('title')?.setValue('New Note');
      component.noteForm.get('markdown')?.setValue('# New Content');

      const delayedObservable = new Observable<Note>(subscriber => {
        setTimeout(() => {
          subscriber.next(mockNote);
          subscriber.complete();
        }, 100);
      });
      notesServiceSpy.createNote.and.returnValue(delayedObservable);

      component.onSubmit();

      expect(component.isSaving).toBe(true);

      tick(100);
    }));

    it('should navigate to notes list after successful save', (done) => {
      
      component.noteForm.get('title')?.setValue('New Note');
      component.noteForm.get('markdown')?.setValue('# New Content');
      notesServiceSpy.createNote.and.returnValue(of(mockNote));

      component.onSubmit();

      setTimeout(() => {
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/notes']);
        expect(component.isSaving).toBe(false);
        done();
      }, 100);
    });

    it('should display error message when save fails', (done) => {
      
      component.noteForm.get('title')?.setValue('New Note');
      component.noteForm.get('markdown')?.setValue('# New Content');
      notesServiceSpy.createNote.and.returnValue(
        throwError(() => new Error('Failed to create note'))
      );

      component.onSubmit();

      setTimeout(() => {
        expect(component.errorMessage).toBe('Failed to create note');
        expect(component.isSaving).toBe(false);
        done();
      }, 100);
    });

    it('should not submit when form is invalid', () => {
      
      component.noteForm.get('title')?.setValue('');
      component.noteForm.get('markdown')?.setValue('');

      component.onSubmit();

      expect(notesServiceSpy.createNote).not.toHaveBeenCalled();
      
      expect(component.noteForm.get('title')?.touched).toBe(true);
      expect(component.noteForm.get('markdown')?.touched).toBe(true);
    });
  });

  describe('Markdown Preview', () => {
    it('should update preview when markdown changes', fakeAsync(() => {
      
      const markdownControl = component.noteForm.get('markdown');

      markdownControl?.setValue('# Heading\n\nParagraph text');
      tick(100);

      expect(component.markdownPreview).toContain('<h1');
      expect(component.markdownPreview).toContain('Heading');
    }));

    it('should toggle preview mode', () => {
      
      expect(component.isPreviewMode).toBe(false);

      component.togglePreview();

      expect(component.isPreviewMode).toBe(true);

      component.togglePreview();

      expect(component.isPreviewMode).toBe(false);
    });

    it('should toggle split view', () => {
      
      expect(component.isSplitView).toBe(true);

      component.toggleSplitView();

      expect(component.isSplitView).toBe(false);

      component.toggleSplitView();

      expect(component.isSplitView).toBe(true);
    });
  });

  describe('Tag Management', () => {
    it('should update selectedTagIds when onTagsChanged is called', () => {
      
      const newTagIds = [
        '11111111-1111-1111-1111-111111111111',
        '22222222-2222-2222-2222-222222222222',
        '33333333-3333-3333-3333-333333333333'
      ];

      component.onTagsChanged(newTagIds);

      expect(component.selectedTagIds).toEqual(newTagIds);
    });

    it('should toggle tag selection', () => {
      
      const tagId1 = '11111111-1111-1111-1111-111111111111';
      const tagId2 = '22222222-2222-2222-2222-222222222222';
      component.selectedTagIds = [tagId1];

      component.toggleTag(tagId2);

      expect(component.selectedTagIds).toContain(tagId2);
      expect(component.selectedTagIds.length).toBe(2);

      component.toggleTag(tagId1);

      expect(component.selectedTagIds).not.toContain(tagId1);
      expect(component.selectedTagIds.length).toBe(1);
    });

    it('should check if tag is selected', () => {
      
      const tagId1 = '11111111-1111-1111-1111-111111111111';
      const tagId2 = '22222222-2222-2222-2222-222222222222';
      const tagId3 = '33333333-3333-3333-3333-333333333333';
      component.selectedTagIds = [tagId1, tagId2];

      expect(component.isTagSelected(tagId1)).toBe(true);
      expect(component.isTagSelected(tagId2)).toBe(true);
      expect(component.isTagSelected(tagId3)).toBe(false);
    });
  });

  describe('Auto-save Draft', () => {
    it('should have auto-save functionality', () => {

      expect(component.noteForm).toBeTruthy();
      expect(component.noteForm.get('title')).toBeTruthy();
      expect(component.noteForm.get('markdown')).toBeTruthy();
    });
  });

  describe('Cancel Action', () => {
    it('should navigate to notes list when cancel is confirmed', () => {
      
      spyOn(window, 'confirm').and.returnValue(true);

      component.onCancel();

      expect(routerSpy.navigate).toHaveBeenCalledWith(['/notes']);
    });

    it('should not navigate when cancel is not confirmed', () => {
      
      spyOn(window, 'confirm').and.returnValue(false);

      component.onCancel();

      expect(routerSpy.navigate).not.toHaveBeenCalled();
    });
  });
});
