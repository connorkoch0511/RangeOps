-- Optional seed data so the dashboard has something to show on first run.
INSERT INTO missions (name, aircraft, scheduled_start, scheduled_end, status) VALUES
    ('Envelope Expansion 4A', 'F-16C',  now() - interval '2 hours', now() - interval '1 hour', 'COMPLETE'),
    ('Stores Separation 12',  'F/A-18E', now() + interval '1 day',   now() + interval '1 day 2 hours', 'PLANNED'),
    ('Avionics Regression 7', 'T-38C',  now(),                       now() + interval '90 minutes', 'ACTIVE');

INSERT INTO test_runs (mission_id, name, status, started_at, ended_at, notes) VALUES
    (1, 'Climb to FL250',       'PASS', now() - interval '2 hours', now() - interval '110 minutes', 'Nominal'),
    (1, 'Level accel M0.9',     'PASS', now() - interval '105 minutes', now() - interval '95 minutes', 'Nominal'),
    (3, 'Nav database load',    'RUNNING', now() - interval '5 minutes', NULL, NULL);
