# Workspace Rules: Storage & Drive Restrictions

- **Strict D: Drive Storage Only**: All project work, permanent files, temporary/scratch files, build outputs, and caches must stay on the `D:` drive. The `C:` drive has minimal free space and must not be used for any operations.
- **Temporary & Scratch Files**: When running scripts, tests, or generating temporary files, always use a local directory on `D:` (such as `D:\Documents\projects\GridLocked\.tmp` or `D:\tmp`). Do not use Windows default `%TEMP%` or `%TMP%` located on `C:`.
- **Conversation & App Data**: Antigravity conversation databases, transcripts, and model caches are mapped to `D:` to avoid consuming space on `C:`.
