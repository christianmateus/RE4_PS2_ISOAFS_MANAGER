# RE4 PS2 ISO/AFS Manager

A Windows tool for working with **AFS archives and PlayStation 2 ISO
images**, developed primarily for **Resident Evil 4 (PS2) modding**.

The project was created to provide a more convenient workflow for
extracting, replacing, analyzing, and rebuilding files inside AFS
archives --- including AFS files stored directly inside a PS2 ISO.

## Features

-   Open standalone `.AFS` archives.
-   Open PlayStation 2 `.ISO` images and detect AFS archives inside
    them.
-   Browse AFS entries with detailed information such as:
    -   Index
    -   File name and type
    -   Offset
    -   Current Size
    -   Stored Size
    -   Maximum allocated size
    -   Padding
    -   Compact Size
    -   Wasted space
    -   Timestamp
    -   TOC metadata
-   Extract a single file.
-   Extract all files in batch.
-   Replace/import a single file.
-   Import multiple files from an extracted folder.
-   Index-based batch extraction to prevent duplicate file names from
    overwriting each other.
-   Generate an `afs_manifest.txt` file for reliable batch re-importing.
-   Detect unchanged files and skip unnecessary writes.
-   Analyze recoverable/wasted space inside an AFS archive.
-   Compact/rebuild AFS archives using the real file sizes while
    preserving required alignment.
-   Rebuild an AFS directly inside a PS2 ISO when the rebuilt archive
    fits within the available ISO extent.
-   Preserve the AFS starting LBA during supported in-place ISO
    rebuilds.
-   Update the ISO9660 file size after rebuilding an AFS inside the ISO.
-   Progress windows for long batch and ISO rebuild operations.
-   Light and dark themes.
-   English and Portuguese interface.
-   Optional success notification popups.
-   Persistent application preferences.

## Why This Tool Exists

Some existing AFS tools allow files to be replaced inside an archive but
keep the previous reserved space even when the replacement file is
smaller.

For modding, this can leave unnecessary space allocated to files and
make archive management inconvenient.

RE4 PS2 ISO/AFS Manager can analyze this unused space and rebuild the
AFS so entries are packed again using their actual sizes and the
required alignment.

For example, an entry may have:

``` text
Current Size : 385728
Max Size     : 690176
```

After replacing it with a smaller file, the old allocation may remain. A
rebuild can repack the archive and recover that unused space.

## Batch Extraction

Batch extraction uses the AFS entry index as part of the output file
name:

``` text
em/
  000507_em1e.snd
  000508_em1e.dat
```

This prevents files with identical names from overwriting each other.

An `afs_manifest.txt` file is also generated. The manifest keeps the
relationship between the extracted file and its original AFS entry,
allowing reliable batch imports later.

## ISO Support

The application can read ISO9660 PS2 images and open AFS archives
directly from their location inside the ISO.

The AFS does not need to be manually extracted first.

For supported in-place rebuilds, the application:

1.  Analyzes the AFS structure.
2.  Builds a compact temporary AFS.
3.  Validates the rebuilt archive.
4.  Writes it back to the original AFS extent inside the ISO.
5.  Clears the unused remainder of the previous extent.
6.  Updates the ISO9660 file size.
7.  Preserves the original starting LBA.

For safety, an in-place ISO rebuild is blocked if the rebuilt AFS would
exceed the space currently available for that ISO file.

## Rebuild / Compact

The rebuild system currently preserves the structure required by the
tested Resident Evil 4 PS2 AFS archives, including:

-   Entry indexes
-   Empty-entry sentinel values
-   AFS TOC
-   File names
-   Timestamps
-   TOC metadata
-   `0x800` alignment

Normal entries are rebuilt using their actual/current size rather than
unnecessarily retaining an oversized previous allocation.

## Requirements

-   Windows
-   .NET 6
-   Windows Forms

## Building

Open the project in a recent version of Visual Studio with the **.NET
desktop development** workload installed.

The project targets:

``` text
net6.0-windows
```

Build the solution normally using Visual Studio.

## Languages

The interface currently supports:

-   English
-   Portuguese (Brazil)

The language can be changed from the application settings menu.

## Important Notes

This project was developed and tested primarily around **Resident Evil 4
for PlayStation 2**.

Although AFS is used by other games and software, archive variants may
differ. Compatibility with unrelated AFS implementations is not
guaranteed.

Always keep backups before modifying an ISO or archive. Direct ISO
import and rebuild operations modify the image itself.

## Project Status

The core workflow has been tested with Resident Evil 4 PS2:

``` text
Open ISO
→ Open AFS
→ Extract files
→ Modify files
→ Import files
→ Analyze space
→ Rebuild/compact AFS
→ Write rebuilt AFS into ISO
→ Run the modified ISO in-game
```

This workflow has been successfully validated in-game during
development.

## Disclaimer

This is an unofficial modding tool and is not affiliated with or
endorsed by Capcom, Sony, or any other rights holder.

No game files, copyrighted game assets, or disc images are included with
this project.

Use the tool only with files you are legally permitted to modify.

## Contributing

Bug reports, compatibility findings, and improvements are welcome.

If you test the tool with other AFS-based PlayStation 2 games, please
include information about the game, AFS structure, and any differences
you observe.
