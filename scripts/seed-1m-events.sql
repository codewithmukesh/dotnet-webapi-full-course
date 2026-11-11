-- Seeds 1,000,000 published events (one General ticket type each) for the pagination chapter.
-- Run: docker compose exec -T postgres psql -U postgres -d ticketing < scripts/seed-1m-events.sql
\timing on

insert into events (id, title, description, venue_name, venue_city, starts_at, status, created_at)
select
    gen_random_uuid(),
    'Event #' || g,
    null,
    'Hall ' || (g % 50),
    (array['Chennai', 'Bengaluru', 'Mumbai', 'Pune', 'Hyderabad', 'Delhi', 'Kochi', 'Kolkata'])[1 + g % 8],
    now() + make_interval(mins => g),
    'Published',
    now()
from generate_series(1, 1000000) g;

insert into ticket_types (id, event_id, name, price, total_quantity, quantity_left, max_per_user)
select gen_random_uuid(), e.id, 'General', 499, 100, 100, 4
from events e
where e.title like 'Event #%';

analyze events;
analyze ticket_types;
