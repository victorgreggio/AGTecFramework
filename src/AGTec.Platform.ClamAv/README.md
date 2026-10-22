# AGTec.Platform.ClamAv

ClamAV adapter for the `AGTec.Platform` `IMalwareScanner` port. `ClamAvMalwareScanner` streams
content to a ClamAV daemon (`clamd`) over its TCP protocol via [nClam]. Selected by configuration in
place of the default `NoOpMalwareScanner` for inline scanning of uploads.

Part of [AGTecFramework](https://www.nuget.org/packages?q=AGTecFramework).

## Install

```bash
dotnet add package AGTec.Platform.ClamAv
```

## What it provides

- `ClamAvMalwareScanner` — `IMalwareScanner` backed by an `IClamClient` (nClam). A clean scan
  returns `MalwareScanResult.Clean`; a detection returns `MalwareScanResult.Infected(virusName)`; a
  failed scan throws `InvalidOperationException`.
- DI helper on `AGTecClamAvExtensions`: `AddClamAvScanner(host, port = 3310)` — registers the
  `IClamClient` and the scanner.

## Usage

```csharp
using AGTec.Platform;
using AGTec.Platform.ClamAv;
using Microsoft.Extensions.DependencyInjection;

services.AddFileSystemStorage("local-data/blobs");   // IObjectStore (any provider)
services.AddClamAvScanner("clamav", 3310);           // IMalwareScanner -> ClamAV
services.ScanObjectStoreForMalware();                // scan every upload before storing
```

With the scanning decorator in place, an infected upload is rejected with
`MalwareDetectedException` and never written to the store.

## Configuration

`AddClamAvScanner` takes host/port directly; the keys below are how the reference composition root
(`samples/AGTec.Reference`) selects and feeds it.

| Key | Purpose | Default |
| --- | --- | --- |
| `AGTec:Storage:Scanner` = `ClamAv` | selects `ClamAvMalwareScanner` | `None` |
| `ConnectionStrings:clamav` | `host:port` of the clamd endpoint | `clamav:3310` |

## Swapping adapters

Register this in place of the default `AddNoOpMalwareScanner()` — one line, consumers unchanged:

```csharp
// default: services.AddNoOpMalwareScanner();  ->
services.AddClamAvScanner(host, port);
services.ScanObjectStoreForMalware();           // decorate the registered IObjectStore
```

The composition root picks it when `AGTec:Storage:Scanner=ClamAv`; otherwise the no-op scanner runs
(scanning delegated to the storage layer). Switching is a config change, not a recompile.

## See also

`AGTec.Platform` (the `IMalwareScanner` port and scanning decorator), `AGTec.Platform.Azure`,
`AGTec.Platform.Dapr`.

[nClam]: https://www.nuget.org/packages/nClam
