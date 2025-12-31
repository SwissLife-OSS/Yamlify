# YAML Test Suite Extension

This folder contains additional test cases that extend the official [yaml-test-suite](https://github.com/yaml/yaml-test-suite).

These tests cover edge cases discovered during real-world usage that are not covered by the official test suite.

## Test Cases

| ID   | Name | Description |
|------|------|-------------|
| NVEM | Null value at end of mapping in sequence | Tests `key:` null value at end of mapping item followed by sibling sequence item |
| NVED | Null value at end of document | Tests `key:` null value when it's the last content in the document |
| NVDN | Null value followed by dedented key | Tests null values in nested mapping followed by dedented sibling keys |
| NVML | Null value in multi-level nesting | Tests null values in deeply nested mapping-in-sequence structures |
| NVMN | Multiple null values in sequence items | Tests multiple sequence items with null value mappings |

## Format

Test files follow the same format as the official yaml-test-suite:

```yaml
---
- name: Test name
  from: '@yamlify'
  tags: tag1 tag2
  yaml: |
    # The YAML to parse
  tree: |
    # Expected event tree
  json: |
    # Expected JSON equivalent
```
