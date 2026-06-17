-- Derived search index over the character archive. Additive: the source *_character_def tables
-- are untouched; this materialized view projects uniform display fields plus a full-text vector.
-- Rebuild after the archive changes with: REFRESH MATERIALIZED VIEW CONCURRENTLY character_search;

CREATE MATERIALIZED VIEW IF NOT EXISTS character_search AS
SELECT
    u.source,
    u.key,
    u.name,
    u.author,
    u.image_hash,
    u.tags,
    u.summary,
    u.description,
    -- Cap the indexed text: a few outlier descriptions exceed the tsvector 1 MB limit, and 200k
    -- chars is far more than full-text search needs. The stored description column stays full.
    to_tsvector('english',
        left(
            coalesce(u.name, '') || ' ' ||
            coalesce(u.author, '') || ' ' ||
            array_to_string(u.tags, ' ') || ' ' ||
            coalesce(u.summary, '') || ' ' ||
            left(coalesce(u.description, ''), 200000)
        , 500000)) AS search
FROM (
    SELECT 'booru' AS source, 'booru:' || id AS key, name, author, image_hash,
           tags,
           summary,
           coalesce(definition #>> '{data,description}', definition ->> 'description', '') AS description
    FROM public.booru_character_def

    UNION ALL
    SELECT 'char_tavern', 'char_tavern:' || increment::text, name, author, image_hash,
           CASE WHEN jsonb_typeof(definition #> '{data,tags}') = 'array'
                THEN ARRAY(SELECT jsonb_array_elements_text(definition #> '{data,tags}')) ELSE '{}'::text[] END,
           NULL::text,
           coalesce(definition #>> '{data,description}', definition ->> 'description', '')
    FROM public.char_tavern_character_def

    UNION ALL
    SELECT 'chub', 'chub:' || increment::text, name, author, image_hash,
           CASE WHEN jsonb_typeof(definition #> '{data,tags}') = 'array'
                THEN ARRAY(SELECT jsonb_array_elements_text(definition #> '{data,tags}')) ELSE '{}'::text[] END,
           NULL::text,
           coalesce(definition #>> '{data,description}', definition ->> 'description', '')
    FROM public.chub_character_def

    UNION ALL
    SELECT 'generic', 'generic:' || card_data_hash, name, NULL::text, image_hash,
           CASE WHEN jsonb_typeof(definition #> '{data,tags}') = 'array'
                THEN ARRAY(SELECT jsonb_array_elements_text(definition #> '{data,tags}')) ELSE '{}'::text[] END,
           summary,
           coalesce(definition #>> '{data,description}', definition ->> 'description', '')
    FROM public.generic_character_def

    UNION ALL
    SELECT 'nyaime', 'nyaime:' || increment::text, name, author, image_hash,
           CASE WHEN jsonb_typeof(definition #> '{data,tags}') = 'array'
                THEN ARRAY(SELECT jsonb_array_elements_text(definition #> '{data,tags}')) ELSE '{}'::text[] END,
           NULL::text,
           coalesce(definition #>> '{data,description}', definition ->> 'description', '')
    FROM public.nyaime_character_def

    UNION ALL
    SELECT 'risuai', 'risuai:' || increment::text, name, author, image_hash,
           CASE WHEN jsonb_typeof(definition #> '{data,tags}') = 'array'
                THEN ARRAY(SELECT jsonb_array_elements_text(definition #> '{data,tags}')) ELSE '{}'::text[] END,
           NULL::text,
           coalesce(definition #>> '{data,description}', definition ->> 'description', '')
    FROM public.risuai_character_def

    UNION ALL
    SELECT 'webring', 'webring:' || card_data_hash, name, author, image_hash,
           CASE WHEN jsonb_typeof(definition #> '{data,tags}') = 'array'
                THEN ARRAY(SELECT jsonb_array_elements_text(definition #> '{data,tags}')) ELSE '{}'::text[] END,
           summary,
           coalesce(definition #>> '{data,description}', definition ->> 'description', '')
    FROM public.webring_character_def
) u;

-- Unique key index enables REFRESH MATERIALIZED VIEW CONCURRENTLY.
CREATE UNIQUE INDEX IF NOT EXISTS character_search_key_idx  ON character_search (key);
CREATE INDEX        IF NOT EXISTS character_search_fts_idx  ON character_search USING GIN (search);
CREATE INDEX        IF NOT EXISTS character_search_tags_idx ON character_search USING GIN (tags);
CREATE INDEX        IF NOT EXISTS character_search_name_idx ON character_search (lower(name));
