# Search & Caching

## Meilisearch

**Meilisearch** is integrated for advanced query search capabilities. It allows for full-text search and complex querying that goes beyond standard SQL capabilities.

- Used for: High-performance search queries.
- Integration: The `Infrastructure` layer handles the synchronization and querying of Meilisearch.

## Valkey Cache

**Valkey** (a high-performance key-value store, fork of Redis) is used for caching to improve application performance.

- Used for: Caching frequently accessed data to reduce database load.
- Implementation: Distributed caching support.
