# 🤖 Scanotron 2000

**Scanotron 2000** is an AI-powered pagewise PDF processing tool that extracts, analyzes, and transforms PDF content using customizable AI prompts. Built with .NET 9 and Microsoft Semantic Kernel, it supports multiple AI providers and offers specialized processing modes for different document analysis needs.

## 🤔 Why?

I built this to unmerge large PDFs back into multiple logical documents with the "pagebreaker" prompt.

```
> ./scanotron '/pdfs/4 documents merged.pdf' --model qwen/qwen3-4b-2507 --prompt pagebreaker --format "{answer}\nPage {pageNumber}\n"

🔍 Processing PDF: 4 documents merged.pdf
🤖 Using model: qwen/qwen3-4b-2507 from http://localhost:1234
📝 Using prompt template: pagebreaker

📄 Total pages: 6


Page 1
---BREAK-NEW-DOCUMENT---
Page 2

Page 3

Page 4
---BREAK-NEW-DOCUMENT---
Page 5
---BREAK-NEW-DOCUMENT---
Page 6
```

## What can it do?

Scanotron 2000 lets you apply LLM prompts to each page of a PDF file. Some use cases might be ...

### Document Splitting & Analysis
- **Page Breaking**: Use the `pagebreaker` prompt to intelligently identify logical document boundaries in merged PDFs
- **Document Classification**: Automatically detect document types and categorize content

### Data Extraction
- **Structured Data**: Extract phone numbers, email addresses, dates, and other specific information
- **Entity Recognition**: Identify people, organizations, locations, and key entities

### Content Summarization
- **Executive Summaries**: Generate concise overviews of lengthy documents
- **Key Points**: Extract main ideas and important information from each page
- **Headline Generation**: Create descriptive titles for document sections

### Automation-Ready Processing
- **Clean Output**: Default output is script-friendly and parseable
- **Verbose Mode**: Use `--verbose` for detailed processing information
- **Custom Formatting**: Template-based output for integration with other tools

```bash
# Generate document summaries
./scanotron report.pdf --prompt "Summarize this text in to sentences"

# Split merged documents intelligently
./scanotron merged-docs.pdf --prompt pagebreaker

# Extract contact information
./scanotron business-cards.pdf --prompt "Extract phone numbers and emails" --verbose
```

## Quick Start

### Prerequisites

- An OpenAI compatible endpoint, which includes proprietary servies like ChatGPT as well as local servers server as LM Studio, Ollama, etc.
- [.NET 9.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) for the framework-dependent executable (not required for the self-contained one)

If you prefer to run the app from code, you'll need [git](https://git-scm.com/) and the [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) instead of the runtime.

### Basic Usage

```bash
# Extract headlines from each page
./scanotron document.pdf --prompt headliner

# Use custom direct prompts (no predefined template needed)
./scanotron document.pdf --prompt "Summarize the main points in bullet format"

# Find logical document breaks to split up merged documents
./scanotron large-document.pdf --prompt pagebreaker

# Generate JSON metadata for each page
./scanotron report.pdf --prompt json-derulo --verbose

# Show detailed processing information
./scanotron document.pdf --prompt headliner --verbose

```

Running a self compiled version from source code requires `dotnet run --` instead of `scanotron`.

```bash
# dotnet run is compiling an running scanotron on your machine
# the -- afterwards tells the command line that dotnet run doesnt get any command line agruments
# the command line arguments after -- are meant for the scanotron executable
dotnet run -- document.pdf --prompt headliner
```

## Configuration

### AI Provider Setup

#### LM Studio (Default)
```bash
# Start LM Studio and load a model
# Default endpoint: http://localhost:1234
./scanotron document.pdf --prompt headliner
```

#### Ollama
```bash
./scanotron document.pdf --prompt headliner --endpoint http://localhost:11434
```

#### OpenAI
```bash
./scanotron document.pdf --prompt headliner --endpoint https://api.openai.com --apikey your-api-key
```

#### Custom OpenAI-Compatible API
```bash
./scanotron document.pdf --prompt headliner --endpoint http://your-server:8080 --model your-model
```

### Command Line Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--prompt` | `-p` | Prompt name or direct prompt text (required) | - |
| `--format` | `-f` | Output format template with .NET formatting | `Page {pageNumber}:\n{answer}\n` |
| `--verbose` | `-v` | Show detailed processing information | - |
| `--model` | `-m` | Model name | Auto-detected |
| `--endpoint` | `-e` | API endpoint URL | `http://localhost:1234` |
| `--apikey` | `-k` | API key | `API_KEY` env var |
| `--list-prompts` | - | List available prompts | - |
| `--help` | `-h` | Show help | - |

#### Format Template Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `{pageNumber}` | Current page number | `1` |
| `{pageCount}` | Total number of pages | `42` |
| `{pageText}` | Raw text content of the page | Full page text |
| `{previousPageText}` | Text content of the previous page | Previous page text |
| `{answer}` | AI-generated response | AI output |
| `{pageDuration}` | Time to process this page | `1.245` |
| `{totalDuration}` | Total processing time | `02:35` |
| `{now}` | Current local date and time | `2025-10-08 14:30:45` |
| `{utcNow}` | Current UTC date and time | `2025-10-08 12:30:45` |

#### Format String Examples

```bash
# Basic formatting
--format "Page {pageNumber}: {answer}\n"

# With zero-padded page numbers
--format "Page {pageNumber:D3}/{pageCount:D3}: {answer}\n"

# With timestamps
--format "[{now:yyyy-MM-dd HH:mm:ss}] Page {pageNumber}: {answer}\n"
--format "[{utcNow:yyyy-MM-dd HH:mm:ss} UTC] {answer}\n"

# Date/time only formats
--format "[{now:yyyy-MM-dd}] {answer}\n"
--format "[{now:HH:mm:ss}] {answer}\n"
--format "[{utcNow:yyyy-MM-dd}] {answer}\n"
--format "[{utcNow:HH:mm:ss}] {answer}\n"

# With duration tracking
--format "Page {pageNumber} ({pageDuration:ss\\.fff}s): {answer}\n"
--format "{pageNumber:D2}. {answer} [Total: {totalDuration:mm\\:ss}]\n"

# CSV format
--format "{pageNumber},{now:yyyy-MM-dd},{answer}\n"
```

The format template supports standard .NET string formatting for all variables.

## Advanced Features

### Direct Prompt Mode
Instead of using predefined prompt templates, you can provide direct prompt text:

```bash
# Direct summarization prompt
./scanotron document.pdf --prompt "Summarize the content of this page in one sentence"

# Direct analysis prompt  
./scanotron document.pdf --prompt "Extract all phone numbers and email addresses from this page"

# Direct translation prompt
./scanotron document.pdf --prompt "Translate this text to French"
```

### Output Formatting
Customize the output format using template variables and .NET formatting:

```bash
# Professional report format
./scanotron document.pdf --prompt headliner --format "Page {pageNumber} of {pageCount}: {answer}\n\n"

# With timestamps
./scanotron document.pdf --prompt headliner --format "[{now:yyyy-MM-dd HH:mm}] {answer}\n"

# With UTC timestamps and duration
./scanotron document.pdf --prompt headliner --format "{pageNumber:D3} | {utcNow:HH:mm:ss} UTC | {pageDuration:ss\\.fff}s | {answer}\n"

# Minimal clean output (default behavior)
./scanotron document.pdf --prompt headliner --format "{answer}\n"

# CSV-like format (escape commas in content)
./scanotron document.pdf --prompt headliner --format "{pageNumber},{now:yyyy-MM-dd},{answer}\n"
```

### Clean Processing
By default, output is clean and suitable for automation and piping:

```bash
# Pipe to file
./scanotron document.pdf --prompt headliner > results.txt

# Use in scripts
HEADLINES=$(./scanotron document.pdf --prompt headliner --format "{answer} ")
```

## Custom Prompts

Create custom prompts by adding YAML files to the `Prompts/` directory:

```yaml
name: your-custom-prompt
template_format: semantic-kernel
description: Your prompt description

input_variables:
  - name: pageText
    description: The text content of the page
    is_required: true

execution_settings:
  default:
    temperature: 0.7

template: |
  Your custom prompt template here.
  Use {{ $pageText }} to access the page content.
```

## How It Works

1. **PDF Text Extraction**: Uses iText7 to extract text content from PDF pages
2. **AI Processing**: Sends extracted text to AI models via OpenAI-compatible APIs
3. **Prompt Application**: Applies specialized prompts using Microsoft Semantic Kernel
4. **Result Output**: Returns processed results based on the selected prompt
