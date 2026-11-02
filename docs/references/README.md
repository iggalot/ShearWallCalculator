# Reference documents

| File | What | Source | Downloaded |
|---|---|---|---|
| `C-C-2026.pdf` | Simpson Strong-Tie, *Wood Construction Connectors* catalog **C-C-2026** (2026–2027 edition, expires 12/31/27), 388 pages, 61 MB | Simpson Strong-Tie's own page, https://www.strongtie.com/resources/literature/wood-construction-connectors-catalog ("Download Complete Catalog"); file from Simpson's asset host, https://ssttoolbox.widen.net/content/orplhjaqw1/original/C-C-2026.pdf | 2026-11-02, SHA-256 `5b5fce254e8995b54de0b09380ee0677630dfcff71e6186a80cfa5995fa9f102` |

All hold-down values in `NDSProvisions/SimpsonCatalog/*_Manager.cs` come from this copy:
- HDUE / DTT: p. 61.
- LTT / HTT / HTTH: p. 63.
- LSTHD / STHD: p. 67.
- Strap ties: p. 291.

The PDF's page numbers are the catalog's printed page numbers. When Simpson publishes a new edition, download it here and update the pages:
- in `SimpsonCatalog.Edition` and the `Pages*` constants;
- in each manager's citations;
- in `SimpsonCatalogTests`.
