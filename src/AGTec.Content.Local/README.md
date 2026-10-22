# AGTec.Content.Local

The local, cloud-free text-extraction adapter for [`AGTec.Content`](https://www.nuget.org/packages/AGTec.Content).
Implements `ITextExtractor` for plain text/markdown, PDF (via PdfPig), and Word `.docx` (via OpenXml).

Part of [AGTecFramework](https://www.nuget.org/packages?q=AGTecFramework).

## Install

```bash
dotnet add package AGTec.Content.Local
# brings in AGTec.Content (the ITextExtractor / ITextChunker ports)
```

## What it provides

- `LocalTextExtractor` — an `ITextExtractor` with no cloud dependency. It classifies by file extension
  and content type, then extracts: PDF via PdfPig, `.docx` via OpenXml, everything else as UTF-8 text.
- DI helper (`AGTecContentLocalExtensions`): `AddLocalTextExtractor()` registers it as the
  `ITextExtractor`.

## Usage

```csharp
using Microsoft.Extensions.DependencyInjection;
using AGTec.Content;
using AGTec.Content.Local;

services.AddLocalTextExtractor();                              // ITextExtractor
services.AddParagraphChunker(o => o.TargetCharacters = 1200);  // ITextChunker (from AGTec.Content)

public sealed class Ingestor(ITextExtractor extractor, ITextChunker chunker)
{
    public async Task<IReadOnlyList<TextChunk>> IngestAsync(byte[] bytes, string contentType, string fileName, CancellationToken ct)
    {
        var text = await extractor.ExtractAsync(bytes, contentType, fileName, ct);
        return chunker.Chunk(text);
    }
}
```

## Swapping the extractor

`ITextExtractor` is a single-method port defined in `AGTec.Content`. Register a cloud adapter (e.g. Azure
Content Understanding) in place of `AddLocalTextExtractor()` — no code change in consumers.

## See also

[`AGTec.Content`](https://www.nuget.org/packages/AGTec.Content) (the extraction/chunking ports),
`AGTec.Ai` (embeddings), `AGTec.VectorData` (vector store) — the other RAG building blocks.
