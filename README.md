# Scanotron 2000 🚀

**Scanotron 2000** is an AI-powered pagewise PDF processing tool that extracts, analyzes, and transforms PDF content using customizable AI prompts. Built with .NET 9 and Microsoft Semantic Kernel, it supports multiple AI providers and offers specialized processing modes for different document analysis needs.

## 🤔 Why?

I built this to unmerge large PDFs back into multiple logical documents with the "pagebreaker" prompt.

## 🚀 Quick Start

### Prerequisites

Precompiled executable
- [.NET 9.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- An AI model server (LM Studio, Ollama, etc.) or OpenAI compatible API access

Self compiled
- [git](https://git-scm.com/)
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An AI model server (LM Studio, Ollama, etc.) or OpenAI compatible API access

### Basic Usage

```bash
# Extract headlines from each page
./scanotron document.pdf --prompt headliner

# Use custom direct prompts (no predefined template needed)
./scanotron document.pdf --prompt "Summarize the main points in bullet format"

# Find logical document breaks to split up merged documents
./scanotron large-document.pdf --prompt pagebreaker

# Generate JSON metadata for each page
./scanotron report.pdf --prompt json-derulo

# Clean output without informational messages
./scanotron document.pdf --prompt headliner --no-intro

```

Running a self compiled version from source code requires `dotnet run --` instead of `scanotron`.

```bash
# dotnet run is compiling an running scanotron on your machine
# the -- afterwards tells the command line that dotnet run doesnt get any command line agruments
# the command line arguments after -- are meant for the scanotron executable
dotnet run -- document.pdf --prompt headliner
```

## 🔧 Configuration

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
| `--no-intro` | - | Suppress all informational output | - |
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

The format template supports standard .NET string formatting for numeric values.

## � Advanced Features

### Direct Prompt Mode
Instead of using predefined prompt templates, you can provide direct prompt text:

```bash
# Direct German prompt
./scanotron document.pdf --prompt "Fasse den Inhalt dieser Seite in einem Satz zusammen"

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

# Minimal clean output
./scanotron document.pdf --prompt headliner --format "{answer}\n" --no-intro

# CSV-like format (escape commas in content)
./scanotron document.pdf --prompt headliner --format "{pageNumber},{answer}\n" --no-intro
```

### Silent Processing
Use `--no-intro` for clean output suitable for automation and piping:

```bash
# Pipe to file
./scanotron document.pdf --prompt headliner --no-intro > results.txt

# Use in scripts
HEADLINES=$(./scanotron document.pdf --prompt headliner --format "{answer} " --no-intro)
```

## �🛠️ Custom Prompts

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

## 🔍 How It Works

1. **PDF Text Extraction**: Uses iText7 to extract text content from PDF pages
2. **AI Processing**: Sends extracted text to AI models via OpenAI-compatible APIs
3. **Prompt Application**: Applies specialized prompts using Microsoft Semantic Kernel
4. **Result Output**: Returns processed results based on the selected prompt
