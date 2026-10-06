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
        var configuredPath = _configuration["AI:PythonExecutable"];

        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var p1 = Path.GetFullPath(configuredPath, Directory.GetCurrentDirectory());
            if (File.Exists(p1)) return p1;

            var p2 = Path.GetFullPath(configuredPath, AppContext.BaseDirectory);
            if (File.Exists(p2)) return p2;
        }

        var searchRoots = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
        foreach (var root in searchRoots)
        {
            var dir = new DirectoryInfo(root);
            while (dir != null)
            {
                var candidateWin = Path.Combine(dir.FullName, "ai", ".venv", "Scripts", "python.exe");
                if (File.Exists(candidateWin)) return candidateWin;

                var candidateUnix = Path.Combine(dir.FullName, "ai", ".venv", "bin", "python");
                if (File.Exists(candidateUnix)) return candidateUnix;

                dir = dir.Parent;
            }
        }

        return "python";
    }

    // ============================================================
    // Resolve AI script
    // ============================================================

    private string GetScriptPath()
    {
        var configuredPath = _configuration["AI:ScriptPath"];

        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var p1 = Path.GetFullPath(configuredPath, Directory.GetCurrentDirectory());
            if (File.Exists(p1)) return p1;

            var p2 = Path.GetFullPath(configuredPath, AppContext.BaseDirectory);
            if (File.Exists(p2)) return p2;
        }

        var searchRoots = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
        foreach (var root in searchRoots)
        {
            var dir = new DirectoryInfo(root);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "ai", "gemini_analyzer.py");
                if (File.Exists(candidate)) return candidate;

                dir = dir.Parent;
            }
        }

        throw new FileNotFoundException(
            "Python AI script was not found in 'ai/gemini_analyzer.py'.");
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
    // Job Description Structuring (Task A)
    // ============================================================

    public async Task<string> StructureJobAsync(
        string rawJobDescription,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rawJobDescription))
        {
            return "{}";
        }

        var pythonExecutable = GetPythonExecutable();
        var scriptPath = GetScriptPath();
        var workingDirectory = Path.GetDirectoryName(scriptPath)!;

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
                from job_structurer import run_structurer_api
                raw_text = sys.stdin.read()
                result = run_structurer_api(raw_text)

            print(json.dumps(result, default=str))
            """
        );

        using var process = new Process
        {
            StartInfo = psi
        };

        _logger.LogInformation(
            "Starting Python job structurer using {PythonExecutable}",
            pythonExecutable);

        if (!process.Start())
        {
            throw new InvalidOperationException(
                "Failed to start Python job structurer process.");
        }

        await process.StandardInput.WriteAsync(rawJobDescription);
        await process.StandardInput.FlushAsync();
        process.StandardInput.Close();

        var outputTask = process.StandardOutput.ReadToEndAsync(ct);
        var errorTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            _logger.LogError(
                "Python job structurer failed. ExitCode={ExitCode}, Error={Error}",
                process.ExitCode,
                error);

            throw new InvalidOperationException(
                $"Python job structurer failed: {error}");
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            return "{}";
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