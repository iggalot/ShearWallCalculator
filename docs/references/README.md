# Reference documents

| File | What | Source | Downloaded |
|---|---|---|---|
| `C-C-2026.pdf` | Simpson Strong-Tie, *Wood Construction Connectors* catalog **C-C-2026** (2026–2027 edition, expires 12/31/27), 388 pages, 61 MB | Simpson Strong-Tie's own page, https://www.strongtie.com/resources/literature/wood-construction-connectors-catalog ("Download Complete Catalog"); file from Simpson's asset host, https://ssttoolbox.widen.net/content/orplhjaqw1/original/C-C-2026.pdf | 2026-11-02, SHA-256 `5b5fce254e8995b54de0b09380ee0677630dfcff71e6186a80cfa5995fa9f102` |
| `L-C-HDUECONV25.pdf` | Simpson Strong-Tie engineering letter **L-C-HDUECONV25**, "HDUE™ Holdown One-for-One Replacement for Discontinued HDU", April 2, 2025 (valid to 12/31/2026), 1 page. It says Simpson "is discontinuing the HDU holdown series, replacing it with the HDUE series"; Table 1 gives each HDU model's max. allowable loads and its HDUE replacement, and Table 2 the dimensions. | Linked from Simpson's SE Blog post "Unveiling the HDUE Holdown" (https://seblog.strongtie.com/2025/06/unveiling-the-hdue-holdown-engineering-marvels-and-rigorous-testing/); file from https://ssttoolbox.widen.net/content/y6hsczpwvv/original/L-C-HDUECONV25.pdf | 2026-11-02, SHA-256 `d1b2360acb5d1e5f275cdbfe434bf7addea2e652fb60436eb7828e6d4e0796a4` |

All hold-down values in `NDSProvisions/SimpsonCatalog/*_Manager.cs` come from this copy:
- HDUE / DTT: p. 61.
- LTT / HTT / HTTH: p. 63.
- LSTHD / STHD: p. 67.
- Strap ties: p. 291.

The PDF's page numbers are the catalog's printed page numbers. When Simpson publishes a new edition, download it here and update the pages:
- in `SimpsonCatalog.Edition` and the `Pages*` constants;
- in each manager's citations;
- in `SimpsonCatalogTests`.

The discontinued HDU-SDS2.5 hold-downs (`HDU_Legacy_Manager.cs`, family "HDU (discontinued)") are not in C-C-2026. Their values are from C-C-2021 p. 53, checked 2026-11-02; no copy of C-C-2021 is kept here. Each model's largest option and its dimensions also match `L-C-HDUECONV25.pdf`, Tables 1 and 2.
