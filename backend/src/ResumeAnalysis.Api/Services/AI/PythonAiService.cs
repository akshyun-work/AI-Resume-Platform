using System.Diagnostics;
using ResumeAnalysis.Api.Services.Storage;

namespace ResumeAnalysis.Api.Services.AI;

public class PythonAiService : IPythonAiService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PythonAiService> _logger;
    private readonly IFileStorage _fileStorage;

    public PythonAiService(
        IConfiguration configuration,
        ILogger<PythonAiService> logger,
        IFileStorage fileStorage)
    {
        _configuration = configuration;
        _logger = logger;
        _fileStorage = fileStorage;
    }

    // ============================================================
    // Resolve Python executable
    // ============================================================

    private string GetPythonExecutable()
    {
        var configuredPath =
            _configuration["AI:PythonExecutable"];

        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new InvalidOperationException(
                "AI:PythonExecutable is not configured.");
        }

        var fullPath = Path.GetFullPath(
            configuredPath,
            Directory.GetCurrentDirectory());

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                "Configured Python executable was not found.",
                fullPath);
        }

        return fullPath;
    }

    // ============================================================
    // Resolve AI script
    // ============================================================

    private string GetScriptPath()
    {
        var configuredPath =
            _configuration["AI:ScriptPath"];

        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new InvalidOperationException(
                "AI:ScriptPath is not configured.");
        }

        var fullPath = Path.GetFullPath(
            configuredPath,
            Directory.GetCurrentDirectory());

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                "Python AI script was not found.",
                fullPath);
        }

        return fullPath;
    }

    // ============================================================
    // Resume ATS / AI Analysis
    // ============================================================

    public async Task<string> AnalyzeResumeAsync(
        string pdfPath,
        string jobDescription,
        object? jobData,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(pdfPath))
        {
            throw new ArgumentException(
                "PDF path is required.",
                nameof(pdfPath));
        }

        var fullPdfPath =
            _fileStorage.GetFullPath(pdfPath);

        if (!File.Exists(fullPdfPath))
        {
            throw new FileNotFoundException(
                "Resume PDF not found.",
                fullPdfPath);
        }

        var pythonExecutable =
            GetPythonExecutable();

        var scriptPath =
            GetScriptPath();

        var workingDirectory =
            Path.GetDirectoryName(scriptPath)!;

        var psi = new ProcessStartInfo
        {
            FileName = pythonExecutable,
            WorkingDirectory = workingDirectory,

            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,

            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.ArgumentList.Add("-c");

        psi.ArgumentList.Add(
            """
            import sys
            import json
            import contextlib

            with contextlib.redirect_stdout(sys.stderr):
                from gemini_analyzer import run_api_pipeline

                request = json.loads(sys.stdin.read())

                result = run_api_pipeline(
                    pdf_path=request["pdf_path"],
                    job_description=request["job_description"],
                    job_data=request.get("job")
                )

            print(json.dumps(result, default=str))
            """
        );

        using var process = new Process
        {
            StartInfo = psi
        };

        _logger.LogInformation(
            "Starting Python AI analysis using {PythonExecutable}",
            pythonExecutable);

        _logger.LogInformation(
            "Python AI script directory: {WorkingDirectory}",
            workingDirectory);

        if (!process.Start())
        {
            throw new InvalidOperationException(
                "Failed to start Python AI process.");
        }

        var requestJson =
            System.Text.Json.JsonSerializer.Serialize(
                new
                {
                    pdf_path = fullPdfPath,
                    job_description = jobDescription,
                    job = jobData
                });

        await process.StandardInput.WriteAsync(
            requestJson);

        await process.StandardInput.FlushAsync();

        process.StandardInput.Close();

        var outputTask =
            process.StandardOutput.ReadToEndAsync(ct);

        var errorTask =
            process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        var output =
            await outputTask;

        var error =
            await errorTask;

        if (process.ExitCode != 0)
        {
            _logger.LogError(
                "Python AI process failed. ExitCode={ExitCode}, Error={Error}",
                process.ExitCode,
                error);

            throw new InvalidOperationException(
                $"Python AI analysis failed: {error}");
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            throw new InvalidOperationException(
                "Python AI returned empty output.");
        }

        return output.Trim();
    }

    // ============================================================
    // Resume Chatbot
    // ============================================================

    public async Task<string> ChatAsync(
        string pdfPath,
        string message,
        object? conversation,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(pdfPath))
        {
            throw new ArgumentException(
                "PDF path is required.",
                nameof(pdfPath));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "Chat message is required.",
                nameof(message));
        }

        var fullPdfPath =
            _fileStorage.GetFullPath(pdfPath);

        if (!File.Exists(fullPdfPath))
        {
            throw new FileNotFoundException(
                "Resume PDF not found.",
                fullPdfPath);
        }

        var pythonExecutable =
            GetPythonExecutable();

        var scriptPath =
            GetScriptPath();

        var workingDirectory =
            Path.GetDirectoryName(scriptPath)!;

        var psi = new ProcessStartInfo
        {
            FileName = pythonExecutable,
            WorkingDirectory = workingDirectory,

            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,

            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.ArgumentList.Add("-c");

        psi.ArgumentList.Add(
            """
            import sys
            import json
            import contextlib

            with contextlib.redirect_stdout(sys.stderr):
                from resume_chatbot import run_api_chatbot

                request = json.loads(sys.stdin.read())

                result = run_api_chatbot(
                    pdf_path=request["pdf_path"],
                    user_question=request["message"],
                    conversation=request.get("conversation", [])
                )

            print(json.dumps(result, default=str))
            """
        );

        using var process = new Process
        {
            StartInfo = psi
        };

        _logger.LogInformation(
            "Starting Python resume chatbot using {PythonExecutable}",
            pythonExecutable);

        if (!process.Start())
        {
            throw new InvalidOperationException(
                "Failed to start Python chatbot process.");
        }

        var requestJson =
            System.Text.Json.JsonSerializer.Serialize(
                new
                {
                    pdf_path = fullPdfPath,
                    message,
                    conversation
                });

        await process.StandardInput.WriteAsync(
            requestJson);

        await process.StandardInput.FlushAsync();

        process.StandardInput.Close();

        var outputTask =
            process.StandardOutput.ReadToEndAsync(ct);

        var errorTask =
            process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        var output =
            await outputTask;

        var error =
            await errorTask;

        if (process.ExitCode != 0)
        {
            _logger.LogError(
                "Python chatbot failed. ExitCode={ExitCode}, Error={Error}",
                process.ExitCode,
                error);

            throw new InvalidOperationException(
                $"Python chatbot failed: {error}");
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            throw new InvalidOperationException(
                "Python chatbot returned empty output.");
        }

        return output.Trim();
    }
}