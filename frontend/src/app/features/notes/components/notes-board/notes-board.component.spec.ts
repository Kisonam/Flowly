import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { TranslateModule } from '@ngx-translate/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { NotesBoardComponent } from './notes-board.component';

describe('NotesBoardComponent', () => {
  let component: NotesBoardComponent;
  let fixture: ComponentFixture<NotesBoardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NotesBoardComponent, HttpClientTestingModule, TranslateModule.forRoot()],
      schemas: [NO_ERRORS_SCHEMA]
    })
    .compileComponents();

    fixture = TestBed.createComponent(NotesBoardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
