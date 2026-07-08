-- Core baseline schema. Idempotent (CREATE ... IF NOT EXISTS) so it is a no-op on a database that
-- already carried this schema before the migration runner existed. No seed data here — the former
-- 10k demo-row Documents seed was intentionally removed from the schema path.

CREATE TABLE IF NOT EXISTS Documents (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Title TEXT NOT NULL,
  Kind TEXT NOT NULL,
  Subtitle TEXT NULL
);

CREATE INDEX IF NOT EXISTS IX_Documents_Title ON Documents(Title);

CREATE TABLE IF NOT EXISTS DroppedItems (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Kind TEXT NOT NULL,
  Name TEXT NOT NULL,
  OriginalPath TEXT NULL,
  StoredPath TEXT NULL,
  CreatedUtc TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_DroppedItems_CreatedUtc ON DroppedItems(CreatedUtc);

CREATE VIRTUAL TABLE IF NOT EXISTS DroppedItems_fts
USING fts5(Name, OriginalPath, StoredPath, content='DroppedItems', content_rowid='Id');

CREATE TRIGGER IF NOT EXISTS DroppedItems_ai AFTER INSERT ON DroppedItems BEGIN
  INSERT INTO DroppedItems_fts(rowid, Name, OriginalPath, StoredPath)
  VALUES (new.Id, new.Name, new.OriginalPath, new.StoredPath);
END;

CREATE TRIGGER IF NOT EXISTS DroppedItems_ad AFTER DELETE ON DroppedItems BEGIN
  INSERT INTO DroppedItems_fts(DroppedItems_fts, rowid, Name, OriginalPath, StoredPath)
  VALUES('delete', old.Id, old.Name, old.OriginalPath, old.StoredPath);
END;

CREATE TRIGGER IF NOT EXISTS DroppedItems_au AFTER UPDATE ON DroppedItems BEGIN
  INSERT INTO DroppedItems_fts(DroppedItems_fts, rowid, Name, OriginalPath, StoredPath)
  VALUES('delete', old.Id, old.Name, old.OriginalPath, old.StoredPath);
  INSERT INTO DroppedItems_fts(rowid, Name, OriginalPath, StoredPath)
  VALUES (new.Id, new.Name, new.OriginalPath, new.StoredPath);
END;

CREATE TABLE IF NOT EXISTS tag_groups (
  id    INTEGER PRIMARY KEY AUTOINCREMENT,
  name  TEXT NOT NULL UNIQUE,
  color TEXT NULL
);

CREATE TABLE IF NOT EXISTS tags (
  id       INTEGER PRIMARY KEY AUTOINCREMENT,
  name     TEXT NOT NULL UNIQUE,
  group_id INTEGER NULL REFERENCES tag_groups(id) ON DELETE SET NULL
);
CREATE INDEX IF NOT EXISTS IX_tags_group_id ON tags(group_id);

CREATE TABLE IF NOT EXISTS items (
  id           INTEGER PRIMARY KEY AUTOINCREMENT,
  kind         TEXT NOT NULL,
  title        TEXT NOT NULL,
  created_at   TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
  imported_at  TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
  content_hash TEXT NULL
);
CREATE INDEX IF NOT EXISTS IX_items_kind       ON items(kind);
CREATE INDEX IF NOT EXISTS IX_items_created_at ON items(created_at);

CREATE TABLE IF NOT EXISTS blobs (
  id           INTEGER PRIMARY KEY AUTOINCREMENT,
  item_id      INTEGER NOT NULL REFERENCES items(id) ON DELETE CASCADE,
  role         TEXT NOT NULL DEFAULT 'primary',
  mime_type    TEXT NOT NULL,
  size_bytes   INTEGER NOT NULL DEFAULT 0,
  storage_uri  TEXT NOT NULL,
  content_hash TEXT NULL
);
CREATE INDEX IF NOT EXISTS IX_blobs_item_id ON blobs(item_id);

CREATE TABLE IF NOT EXISTS item_tags (
  item_id INTEGER NOT NULL REFERENCES items(id) ON DELETE CASCADE,
  tag_id  INTEGER NOT NULL REFERENCES tags(id)  ON DELETE CASCADE,
  PRIMARY KEY (item_id, tag_id)
);
CREATE INDEX IF NOT EXISTS IX_item_tags_tag_id ON item_tags(tag_id);

CREATE TABLE IF NOT EXISTS attribute_defs (
  id         INTEGER PRIMARY KEY AUTOINCREMENT,
  name       TEXT NOT NULL UNIQUE,
  value_type TEXT NOT NULL,
  applies_to TEXT NULL
);

CREATE TABLE IF NOT EXISTS item_attributes (
  item_id    INTEGER NOT NULL REFERENCES items(id)          ON DELETE CASCADE,
  attr_id    INTEGER NOT NULL REFERENCES attribute_defs(id) ON DELETE CASCADE,
  value_text TEXT NULL,
  value_int  INTEGER NULL,
  value_real REAL NULL,
  value_ts   TEXT NULL,
  PRIMARY KEY (item_id, attr_id)
);
CREATE INDEX IF NOT EXISTS IX_item_attributes_attr_id ON item_attributes(attr_id);

CREATE TABLE IF NOT EXISTS sets (
  id          INTEGER PRIMARY KEY AUTOINCREMENT,
  name        TEXT NOT NULL,
  description TEXT NULL,
  is_smart    INTEGER NOT NULL DEFAULT 0,
  created_at  TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
);

CREATE TABLE IF NOT EXISTS set_items (
  set_id   INTEGER NOT NULL REFERENCES sets(id)  ON DELETE CASCADE,
  item_id  INTEGER NOT NULL REFERENCES items(id) ON DELETE CASCADE,
  added_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
  PRIMARY KEY (set_id, item_id)
);
CREATE INDEX IF NOT EXISTS IX_set_items_item_id ON set_items(item_id);

CREATE TABLE IF NOT EXISTS set_rules (
  id       INTEGER PRIMARY KEY AUTOINCREMENT,
  set_id   INTEGER NOT NULL REFERENCES sets(id) ON DELETE CASCADE,
  field    TEXT NOT NULL,
  operator TEXT NOT NULL,
  value    TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS IX_set_rules_set_id ON set_rules(set_id);

CREATE TABLE IF NOT EXISTS item_relations (
  id            INTEGER PRIMARY KEY AUTOINCREMENT,
  from_item_id  INTEGER NOT NULL REFERENCES items(id) ON DELETE CASCADE,
  to_item_id    INTEGER NOT NULL REFERENCES items(id) ON DELETE CASCADE,
  relation_type TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS IX_item_relations_from ON item_relations(from_item_id);
CREATE INDEX IF NOT EXISTS IX_item_relations_to   ON item_relations(to_item_id);

CREATE VIRTUAL TABLE IF NOT EXISTS fts_items
USING fts5(title, body, tags, attributes, content='items', content_rowid='id');

CREATE TRIGGER IF NOT EXISTS fts_items_ai AFTER INSERT ON items BEGIN
  INSERT INTO fts_items(rowid, title, body, tags, attributes)
  VALUES (new.id, new.title, '', '', '');
END;

CREATE TRIGGER IF NOT EXISTS fts_items_ad AFTER DELETE ON items BEGIN
  INSERT INTO fts_items(fts_items, rowid, title, body, tags, attributes)
  VALUES ('delete', old.id, old.title, '', '', '');
END;

CREATE TRIGGER IF NOT EXISTS fts_items_au AFTER UPDATE ON items BEGIN
  INSERT INTO fts_items(fts_items, rowid, title, body, tags, attributes)
  VALUES ('delete', old.id, old.title, '', '', '');
  INSERT INTO fts_items(rowid, title, body, tags, attributes)
  VALUES (new.id, new.title, '', '', '');
END;
