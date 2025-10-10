# pdfbrrr

Throw local or hosted LLMs against PDF pages. pdfbrrr is a command-line tool that processes PDF documents using AI prompts to extract information, generate summaries, analyze sentiment, and more - one page at a time.

## Features

- **Multi-Prompt Processing**: Apply different AI prompts to each page of a PDF document
- **Flexible Output Formatting**: Customize output with .NET-style format strings
- **Multiple AI Backends**: Works with local LLMs (Ollama, LM Studio) and cloud services (OpenAI, Azure OpenAI)
- **Built-in Prompt Library**: Comes with 10+ pre-built prompts for common document analysis tasks
- **CLI Focused**: Designed for automation and integration into workflows
- **Cross-Platform**: Runs on Windows, macOS, and Linux

## Built-in Prompts

pdfbrrr includes several pre-built prompts for common document analysis tasks:

| Prompt | Description |
|--------|-------------|
| `headliner` | Extracts the main headline or title from each page |
| `sir-summarizer` | Summarizes content in the style of a snobbish aristocrat |
| `entity-extractor` | Identifies and categorizes named entities (persons, organizations, locations, etc.) |
| `sentiment-analyzer` | Analyzes emotional tone and sentiment of content |
| `compliance-checker` | Checks content for regulatory compliance issues |
| `document-type-detector` | Classifies the type of document based on content |
| `json-derulo` | Converts structured data to JSON format |
| `pagebreaker` | Identifies logical sections and breaks in content |
| `privacy-scanner` | Detects potential privacy-related information |
| `action-items` | Extracts actionable items and tasks from content |

## Installation

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- An AI model server (local or cloud)

### Building from Source

```bash
git clone https://github.com/your-username/pdfbrrr.git
cd pdfbrrr
dotnet build -c Release
```

### Running

```bash
dotnet run -- [options] <pdf-file>
```

Or after building:

```bash
dotnet bin/Release/net8.0/pdfbrrr.dll [options] <pdf-file>
```

## Quick Start

### Basic Usage

```bash
# Extract headlines from each page
pdfbrrr document.pdf --prompt headliner

# Summarize each page with a specific style
pdfbrrr document.pdf --prompt sir-summarizer

# Use a custom prompt
pdfbrrr document.pdf --prompt "Summarize this content in one sentence"
```

### With Local LLM Server

```bash
# With Ollama (default endpoint)
pdfbrrr document.pdf --prompt headliner --endpoint http://localhost:11434

# With LM Studio (default endpoint)
pdfbrrr document.pdf --prompt headliner --endpoint http://localhost:1234
```

### With Cloud Services

```bash
# With OpenAI
pdfbrrr document.pdf --prompt headliner --endpoint https://api.openai.com --apikey YOUR_API_KEY

# With Azure OpenAI
pdfbrrr document.pdf --prompt headliner --endpoint https://YOUR_RESOURCE.openai.azure.com --apikey YOUR_API_KEY --model gpt-4
```

## Output Formatting

pdfbrrr supports powerful output formatting using .NET-style format strings with custom variables:

### Available Variables

| Variable | Description |
|----------|-------------|
| `{pageNumber}` | Current page number |
| `{pageCount}` | Total number of pages |
| `{pageText}` | Full text content of current page |
| `{previousPageText}` | Text content of previous page |
| `{answer}` | AI-generated response |
| `{now}` | Current local date/time |
| `{utcNow}` | Current UTC date/time |
| `{totalDuration}` | Total processing time |
| `{pageDuration}` | Time to process current page |

### Format Specifiers

All variables support .NET format specifiers:

```bash
# Number formatting
pdfbrrr document.pdf --prompt headliner --format "Page {pageNumber:D3}/{pageCount:D3}: {answer}"

# Date/time formatting
pdfbrrr document.pdf --prompt headliner --format "[{now:yyyy-MM-dd HH:mm}] Page {pageNumber}: {answer}"

# Duration formatting
pdfbrrr document.pdf --prompt headliner --format "Page {pageNumber} ({pageDuration:ss\\.fff}s): {answer}"
```

### Examples

```bash
# Default format
pdfbrrr document.pdf --prompt headliner
# Output: Page 1:\nExtracted headline\nPage 2:\nAnother headline

# Custom format with page numbers
pdfbrrr document.pdf --prompt headliner --format "Page {pageNumber}: {answer}\n"

# Advanced formatting with timestamps
pdfbrrr document.pdf --prompt headliner --format "[{now:HH:mm:ss}] {pageNumber:D2}. {answer}\n"

# JSON-like output
pdfbrrr document.pdf --prompt headliner --format "{{\"page\": {pageNumber}, \"headline\": \"{answer}\"}}\n"
```

## API Compatibility

pdfbrrr works with any OpenAI-compatible API:

### Supported Platforms

- **Ollama**: `--endpoint http://localhost:11434`
- **LM Studio**: `--endpoint http://localhost:1234`
- **OpenAI**: `--endpoint https://api.openai.com --apikey YOUR_KEY`
- **Azure OpenAI**: `--endpoint https://RESOURCE.openai.azure.com --apikey YOUR_KEY`
- **Any OpenAI-compatible server**: `--endpoint YOUR_ENDPOINT --apikey YOUR_KEY`

### Model Selection

```bash
# Auto-detect first available model
pdfbrrr document.pdf --prompt headliner

# Specify a model
pdfbrrr document.pdf --prompt headliner --model gpt-4

# List available models
pdfbrrr --list-models --endpoint http://localhost:1234
```

## Command Line Options

```bash
Usage: pdfbrrr <pdf-file> --prompt <prompt-name> [options]

Arguments:
  <pdf-file>    Path to the PDF file to process

Options:
  --prompt, -p      Prompt name or direct prompt text
  --format, -f      Output format template [default: 'Page {pageNumber}:\n{answer}\n']
  --verbose, -v     Show detailed processing information
  --model, -m       Model name (auto-detected if not specified)
  --endpoint, -e    API endpoint URL [default: http://localhost:1234]
  --apikey, -k      API key (optional, can also use API_KEY env var)
  --list-prompts    List all available prompts and their descriptions
  --help, -h        Show this help
```

## Environment Variables

- `API_KEY`: Default API key for authentication (alternative to `--apikey`)

## Examples

### Document Analysis

```bash
# Extract all headlines
pdfbrrr report.pdf --prompt headliner --verbose

# Generate summaries for each page
pdfbrrr report.pdf --prompt sir-summarizer --format "Page {pageNumber}:\n{answer}\n\n"

# Extract entities from each page
pdfbrrr contract.pdf --prompt entity-extractor

# Analyze sentiment of customer feedback
pdfbrrr feedback.pdf --prompt sentiment-analyzer

# Check for compliance issues
pdfbrrr document.pdf --prompt compliance-checker
```

### Workflow Automation

```bash
# Process multiple documents
for file in *.pdf; do
  pdfbrrr "$file" --prompt headliner > "${file%.pdf}_headlines.txt"
done

# Generate JSON output for further processing
pdfbrrr report.pdf --prompt entity-extractor --format "{{\"page\": {pageNumber}, \"entities\": \"{answer}\"}},\n" > entities.json

# Create a summary report
pdfbrrr report.pdf --prompt sir-summarizer --format "Page {pageNumber}: {answer}\n" > summary.txt
```

## Troubleshooting

### Common Issues

1. **"No models found" error**
   - Ensure your LLM server is running
   - Check the endpoint URL with `--endpoint`
   - Specify a model explicitly with `--model`

2. **Authentication errors**
   - Set API key with `--apikey` or `API_KEY` environment variable
   - Check that your API key has proper permissions

3. **Slow processing**
   - Use `--verbose` to see processing times per page
   - Consider using a smaller model or shorter prompts
   - Check your server's resource usage

### Getting Help

```bash
# Show help
pdfbrrr --help

# List available prompts
pdfbrrr --list-prompts

# List available models
pdfbrrr --list-models
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request
