import assert from 'node:assert/strict';
import test from 'node:test';
import fixtures from './issuesMock.json' with { type: 'json' };
import { advanceIssue, reprioritizeIssue } from './issueModel.ts';

const now = '2026-09-21T12:00:00.000Z';

test('demo fixtures have five unique building reports and valid backend enum values', () => {
  assert.equal(fixtures.length, 5);
  assert.equal(new Set(fixtures.map(issue => issue.issueId)).size, 5);
  for (const issue of fixtures) {
    assert.equal(issue.buildingId, 'building-12');
    assert.ok([1, 2, 3].includes(issue.status));
    assert.ok([1, 2, 3, 4].includes(issue.priority));
    assert.equal(issue.resolvedAt !== null, issue.status === 3);
  }
});

test('an issue progresses independently, with timestamps and no seed mutation', () => {
  const original = structuredClone(fixtures);
  const started = advanceIssue(fixtures, 'issue-101', 1, now);
  assert.equal(started[0].status, 2);
  assert.equal(started[0].updatedAt, now);
  assert.equal(started[0].resolvedAt, null);
  assert.deepEqual(started.slice(1), original.slice(1));
  const closed = advanceIssue(started, 'issue-101', 2, now);
  assert.equal(closed[0].status, 3);
  assert.equal(closed[0].resolvedAt, now);
  assert.deepEqual(fixtures, original);
});

test('stale duplicate actions cannot skip stages or reopen a closed issue', () => {
  const started = advanceIssue(fixtures, 'issue-101', 1, now);
  assert.deepEqual(advanceIssue(started, 'issue-101', 1, now), started);
  assert.deepEqual(advanceIssue(fixtures, 'issue-101', 2, now), fixtures);
  assert.deepEqual(advanceIssue(fixtures, 'issue-105', 3, now), fixtures);
  assert.deepEqual(advanceIssue(fixtures, 'unknown', 1, now), fixtures);
});

test('reprioritization changes only an active issue and preserves closed history', () => {
  const changed = reprioritizeIssue(fixtures, 'issue-101', 4, now);
  assert.equal(changed[0].priority, 4);
  assert.equal(changed[0].updatedAt, now);
  assert.deepEqual(changed.slice(1), fixtures.slice(1));
  assert.deepEqual(reprioritizeIssue(fixtures, 'issue-105', 4, now), fixtures);
  assert.deepEqual(reprioritizeIssue(fixtures, 'issue-101', 3, now), fixtures);
});
