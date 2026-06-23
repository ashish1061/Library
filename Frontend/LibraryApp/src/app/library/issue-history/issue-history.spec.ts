import { ComponentFixture, TestBed } from '@angular/core/testing';

import { IssueHistory } from './issue-history';

describe('IssueHistory', () => {
  let component: IssueHistory;
  let fixture: ComponentFixture<IssueHistory>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [IssueHistory],
    }).compileComponents();

    fixture = TestBed.createComponent(IssueHistory);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
