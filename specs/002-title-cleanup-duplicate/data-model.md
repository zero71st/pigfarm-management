# Data Model: Docs Cleanup

## Entities

- Document
  - path: string (repo path)
  - relativePath: string (repo-root relative)
  - sha1: string
  - title: string (first heading, if any)
  - wordCount: integer
  - headings: string[]
  - references: string[] (other files that link to this document)

- DuplicateCandidate
  - canonicalPath: string
  - otherPaths: string[]
  - similarityScore: number (0..1)
  - action: enum [archive, consolidate, manual-review]

- ArchiveEntry
  - originalPath: string
  - archivePath: string
  - timestamp: string (ISO)
  - commit: string (git commit id)

## Validation rules
- SHA1 must be computed over normalized UTF-8 content.
- Jaccard similarity uses lowercase tokenization on non-word separators.
