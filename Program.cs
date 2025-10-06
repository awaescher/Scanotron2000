using Microsoft.SemanticKernel;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text.Json;

namespace Scanotron2000;

public class ModelInfo
{
    public string Id { get; set; } = string.Empty;
    public string Object { get; set; } = string.Empty;
}

public class ModelsResponse
{
    public string Object { get; set; } = string.Empty;
    public List<ModelInfo> Data { get; set; } = new();
}

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Simple argument parsing
        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
        {
            ShowHelp();
            return 0;
        }

        if (args[0] == "--list-prompts")
        {
            ListAvailablePrompts();
            return 0;
        }

        string pdfPath = args[0];
        string? specifiedModel = GetArgValue(args, "--model", "-m");
        string? promptName = GetArgValue(args, "--prompt", "-p");
        string endpoint = GetArgValue(args, "--endpoint", "-e") ?? "http://localhost:1234";
        string? apiKey = GetArgValue(args, "--apikey", "-k") ?? Environment.GetEnvironmentVariable("API_KEY");

        if (!File.Exists(pdfPath))
        {
            Console.WriteLine($"❌ Error: PDF file '{pdfPath}' not found.");
            return 1;
        }

        if (string.IsNullOrEmpty(promptName))
        {
            Console.WriteLine("❌ Error: The --prompt parameter is required.");
            Console.WriteLine("💡 Example: Scanotron2000 document.pdf --prompt headliner");
            return 1;
        }

        // Get available models or use specified model
        string model;
        if (string.IsNullOrEmpty(specifiedModel))
        {
            Console.WriteLine($"🔍 Discovering available models from {endpoint}...");
            var availableModels = await GetAvailableModelsAsync(endpoint, apiKey);
            
            if (availableModels.Count == 0)
            {
                Console.WriteLine($"❌ No models found at {endpoint}. Please specify a model with --model or start your model server.");
                return 1;
            }

            model = availableModels[0];
            Console.WriteLine($"🤖 Using first available model: {model}");
            
            if (availableModels.Count > 1)
            {
                Console.WriteLine($"💡 Other available models: {string.Join(", ", availableModels.Skip(1))}");
            }
        }
        else
        {
            model = specifiedModel;
        }

        await ProcessPdfAsync(new FileInfo(pdfPath), promptName, model, endpoint, apiKey);
        return 0;
    }

    static void ShowHelp()
    {
        try
        {
            var kernel = CreateKernel("dummy-model", "http://localhost:1234", null);
            var availablePrompts = GetAvailablePromptsFromKernel(kernel);
            var promptsList = availablePrompts.Count > 0 ? string.Join(", ", availablePrompts) : "No prompts found";
            
            Console.WriteLine("Scanotron 2000 - AI-powered PDF headline extractor");
            Console.WriteLine();
            Console.WriteLine("Usage: Scanotron2000 <pdf-file> --prompt <prompt-name> [options]");
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("  <pdf-file>    Path to the PDF file to process");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine($"  --prompt, -p      Prompt name (required) - available: {promptsList}");
            Console.WriteLine("  --model, -m       Model name (auto-detected if not specified)");
            Console.WriteLine("  --endpoint, -e    API endpoint URL [default: http://localhost:1234]");
            Console.WriteLine("  --apikey, -k      API key (optional, can also use API_KEY env var)");
            Console.WriteLine("  --list-prompts    List all available prompts and their descriptions");
            Console.WriteLine("  --help, -h        Show this help");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  # LM Studio (default)");
            Console.WriteLine("  Scanotron2000 document.pdf --prompt headliner");
            Console.WriteLine("  Scanotron2000 document.pdf --prompt headliner --model \"llama-3.2-3b-instruct\"");
            Console.WriteLine();
            Console.WriteLine("  # Ollama");
            Console.WriteLine("  Scanotron2000 document.pdf --prompt headliner --endpoint http://localhost:11434");
            Console.WriteLine();
            Console.WriteLine("  # OpenAI");
            Console.WriteLine("  Scanotron2000 document.pdf --prompt headliner --endpoint https://api.openai.com --apikey your-key");
            Console.WriteLine("  Scanotron2000 document.pdf --prompt headliner --endpoint https://api.openai.com --apikey your-key");
            Console.WriteLine();
            Console.WriteLine("  # Any OpenAI-compatible API");
            Console.WriteLine("  Scanotron2000 document.pdf --prompt headliner --endpoint http://your-server:8080 --model your-model");
            Console.WriteLine();
            Console.WriteLine("Note: When no model is specified, the first available model will be used automatically.");
        }
        catch
        {
            // Fallback if kernel creation fails
            Console.WriteLine("Scanotron 2000 - AI-powered PDF headline extractor");
            Console.WriteLine();
            Console.WriteLine("Usage: Scanotron2000 <pdf-file> --prompt <prompt-name> [options]");
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("  <pdf-file>    Path to the PDF file to process");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --prompt, -p      Prompt name (required)");
            Console.WriteLine("  --model, -m       Model name (auto-detected if not specified)");
            Console.WriteLine("  --endpoint, -e    API endpoint URL [default: http://localhost:1234]");
            Console.WriteLine("  --apikey, -k      API key (optional, can also use API_KEY env var)");
            Console.WriteLine("  --list-prompts    List all available prompts and their descriptions");
            Console.WriteLine("  --help, -h        Show this help");
        }
    }

    static string? GetArgValue(string[] args, params string[] argNames)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (argNames.Contains(args[i]))
            {
                return args[i + 1];
            }
        }
        return null;
    }

    static List<string> GetAvailablePrompts()
    {
        try
        {
            var kernel = CreateKernel("dummy-model", "http://localhost:1234", null);
            return GetAvailablePromptsFromKernel(kernel);
        }
        catch
        {
            return new List<string>();
        }
    }

    static List<string> GetAvailablePromptsFromKernel(Kernel kernel)
    {
        try
        {
            var promptsPlugin = kernel.Plugins["Prompts"];
            return promptsPlugin.Select(f => f.Name).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    static void ListAvailablePrompts()
    {
        Console.WriteLine("Available Prompts:");
        Console.WriteLine("==================");
        Console.WriteLine();

        try
        {
            var kernel = CreateKernel("dummy-model", "http://localhost:1234", null);
            var promptsPlugin = kernel.Plugins["Prompts"];
            
            if (!promptsPlugin.Any())
            {
                Console.WriteLine("❌ No prompt functions found in the Prompts plugin.");
                return;
            }

            foreach (var function in promptsPlugin)
            {
                Console.WriteLine($"📝 {function.Name}");
                Console.WriteLine($"   {function.Description ?? "No description available"}");
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error loading prompts: {ex.Message}");
        }
    }

    static async Task<List<string>> GetAvailableModelsAsync(string endpoint, string? apiKey)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            // Always use OpenAI-compatible /v1/models endpoint
            string modelsUrl = $"{endpoint.TrimEnd('/')}/v1/models";

            // Add authentication header if API key is provided
            if (!string.IsNullOrEmpty(apiKey))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            }

            var response = await httpClient.GetStringAsync(modelsUrl);
            
            // Parse OpenAI-compatible response format
            var modelsResponse = JsonSerializer.Deserialize<ModelsResponse>(response, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            
            return modelsResponse?.Data?.Select(m => m.Id).Where(id => !string.IsNullOrEmpty(id)).ToList() ?? new List<string>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Could not fetch models from {endpoint}: {ex.Message}");
            return new List<string>();
        }
    }

    static async Task ProcessPdfAsync(FileInfo pdfFile, string promptName, string model, string endpoint, string? apiKey)
    {
        try
        {
            Console.WriteLine($"🔍 Processing PDF: {pdfFile.Name}");
            Console.WriteLine($"🤖 Using model: {model} from {endpoint}");
            Console.WriteLine();

            // Initialize Semantic Kernel with OpenAI-compatible API
            var kernel = CreateKernel(model, endpoint, apiKey);

            // Extract text from each page
            using var pdfReader = new PdfReader(pdfFile.FullName);
            using var pdfDocument = new PdfDocument(pdfReader);

            int totalPages = pdfDocument.GetNumberOfPages();
            Console.WriteLine($"📄 Total pages: {totalPages}");
            Console.WriteLine();

            var previousPageText = string.Empty;

            for (int pageNumber = 1; pageNumber <= totalPages; pageNumber++)
            {
                try
                {
                    // Extract text from current page
                    var page = pdfDocument.GetPage(pageNumber);
                    var strategy = new SimpleTextExtractionStrategy();
                    var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);

                    if (string.IsNullOrWhiteSpace(pageText))
                    {
                        Console.WriteLine($"📄 Page {pageNumber}: [No extractable text found]");
                        previousPageText = pageText; // Update for next iteration
                        continue;
                    }

                    // Use AI to extract headline
                    var result = await ProcessPage(kernel, promptName, pageText, pageNumber, totalPages, previousPageText);

                    Console.WriteLine($"Page {pageNumber}:");
                    Console.WriteLine(result);
                    Console.WriteLine();

                    // Store current page text for next iteration
                    previousPageText = pageText;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error processing page {pageNumber}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error processing PDF: {ex.Message}");
        }
    }

    static Kernel CreateKernel(string model, string endpoint, string? apiKey)
    {
        var builder = Kernel.CreateBuilder();

        // Ensure endpoint has proper format for Semantic Kernel
        var baseUri = endpoint.TrimEnd('/');
        if (!baseUri.EndsWith("/v1"))
            baseUri += "/v1";

        // Always use OpenAI-compatible API
        builder.AddOpenAIChatCompletion(
            modelId: model,
            apiKey: apiKey ?? "-", // Use provided API key or placeholder for local servers
            endpoint: new Uri(baseUri));

        builder.Plugins.AddFromPromptDirectoryYaml("Prompts");

        return builder.Build();
    }

    static async Task<string> ProcessPage(Kernel kernel, string promptName, string pageText, int pageNumber, int totalPages, string previousPageText)
    {
        try
        {
            var arguments = new KernelArguments()
            {
                ["pageText"] = pageText,
                ["pageNumber"] = pageNumber,
                ["totalPages"] = totalPages,
                ["previousPageText"] = previousPageText 
            };

            // Get the function from the kernel and invoke it
            var function = kernel.Plugins.GetFunction("Prompts", promptName);
            var result = await kernel.InvokeAsync(function, arguments);
            var headline = result.GetValue<string>()?.Trim();

            return headline ?? string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  AI extraction failed for page {pageNumber}: {ex.Message}");
            return $"Page {pageNumber} Content (AI extraction failed)";
        }
    }
}
