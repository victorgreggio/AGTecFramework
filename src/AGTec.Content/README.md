# AGTec.Content

Text extraction (`ITextExtractor`) and paragraph-aware chunking for RAG ingestion. Ports only — a local
PDF/DOCX adapter (`AGTec.Content.Local`) and cloud adapters plug in behind them, selected by configuration.

Part of [AGTecFramework](https://www.nuget.org/packages?q=AGTecFramework).

## Install

```bash
dotnet add package AGTec.Content
# for PDF/Word extraction:
dotnet add package AGTec.Content.Local
```

## What it provides

- `ITextExtractor` — extracts plain text from a document's bytes. `AGTec.Content` ships a dependency-free
  `PlainTextExtractor` (UTF-8); `AGTec.Content.Local` adds PDF (PdfPig) and Word (.docx via OpenXml).
- `ITextChunker` / `ParagraphTextChunker` — splits text into `TextChunk` records up to a character budget,
  never splitting a paragraph.
- DI helpers: `AddPlainTextExtractor()`, `AddParagraphChunker(...)` (and `AddLocalTextExtractor()` in
  `AGTec.Content.Local`).

## Usage

```csharp
using AGTec.Content;
using AGTec.Content.Local;      // for PDF/DOCX
using Microsoft.Extensions.DependencyInjection;

services.AddLocalTextExtractor();                                  // ITextExtractor
services.AddParagraphChunker(o => o.TargetCharacters = 1200);      // ITextChunker

// in a handler/service:
public sealed class Ingestor(ITextExtractor extractor, ITextChunker chunker)
{
    public async Task<IReadOnlyList<TextChunk>> IngestAsync(byte[] bytes, string contentType, string fileName, CancellationToken ct)
    {
        var text = await extractor.ExtractAsync(bytes, contentType, fileName, ct);
        return chunker.Chunk(text);   // feed each TextChunk.Content to an IEmbeddingGenerator
    }
}
```

## Swapping the extractor

`ITextExtractor` is a single-method port. Register a cloud adapter (e.g. Azure Content Understanding)
in place of `AddLocalTextExtractor()` — no code change in consumers. Implement a token-aware
`ITextChunker` the same way to replace `ParagraphTextChunker`.

## See also

`AGTec.Ai` (embeddings), `AGTec.VectorData` (vector store) — the other RAG building blocks.
