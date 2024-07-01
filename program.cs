using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using LibGit2Sharp;
using System.Dynamic;

internal class Program
{
    public static string SettingsPath = "Settings.json";
    public struct Settings
    {
      public string ProjectName;
      public string ProjectPath;
      public string GitRepository;
      public string[] ExcludedPaths;
      public string[] ExcludedFiles;
      public string[] ExcludedExtensions;
      public string ContextFilePath;
    }
    private static void Main(string[] args)
    {
        // CodeContext is a shell friendly way to build a context window for AI agents to understand the codebase they are working with.
        // Check settings file exists if not ask user relevant questions to create one.

        if (!File.Exists(SettingsPath))
        {
            Console.WriteLine("Settings file not found. Would you like to create one? (Y/N)");
            var response = Console.ReadLine();
            if (response.ToLower() == "y")
            {
                Console.WriteLine("Enter the project name:");
                var projectName = Console.ReadLine();
                Console.WriteLine("Enter the project path:");
                var projectPath = Console.ReadLine();
                Console.WriteLine("Enter the git repository:");
                var gitRepository = Console.ReadLine();

                Console.WriteLine("Enter the context file path:");
                var contextFilePath = Console.ReadLine();
                var excludedPaths = new List<string>();
                excludedPaths.Add("bin");
                excludedPaths.Add("obj");
                excludedPaths.Add(".git");
                excludedPaths.Add("node_modules");
                var excludedFiles = new string[] { ".dll", ".exe", ".pdb", ".zip", ".rar", ".7z", ".tar", ".gz", ".tgz", ".tar.gz", ".bak", ".log", ".tmp", ".temp", ".cache", ".vs", ".vscode", ".git", ".svn", ".hg", ".node_modules", ".bin", ".obj", ".suo", ".user", ".vspscc", ".vssscc", ".vshost.exe", ".vshost.exe.manifest", ".resharper", ".resharper.user", ".resharper.sln", ".resharper.sln.dotsettings", ".resharper.sln.dotsettings" };
                var excludedExtensions = new string[] { ".dll", ".exe", ".pdb", ".zip", ".rar", ".7z", ".tar", ".gz", ".tgz", ".tar.gz", ".bak", ".log", ".tmp", ".temp", ".cache", ".vs", ".vscode", ".git", ".svn", ".hg", ".node_modules", ".bin", ".obj", ".suo", ".user", ".vspscc", ".vssscc", ".vshost.exe", ".vshost.exe.manifest", ".resharper", ".resharper.user", ".resharper.sln", ".resharper.sln.dotsettings", ".resharper.sln.dotsettings" };



                var newSettings = new Settings
                {
                    ProjectName = projectName,
                    ProjectPath = projectPath,
                    GitRepository = gitRepository,
                    ContextFilePath = contextFilePath,
                    ExcludedPaths =  excludedPaths.ToArray(),
                    ExcludedFiles = excludedFiles,
                    ExcludedExtensions = excludedExtensions
                    };
                File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(newSettings));
                Console.WriteLine("Settings file created.");
                // git clone the repository
                // git clone gitRepository projectPath
                try
                {
                    var cloneOptions = new CloneOptions();
                    Repository.Clone(gitRepository, projectPath, cloneOptions);
                    Console.WriteLine("Repository cloned successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }

            }
            else
            {
                return;
            }
        }
        // Open Settings.json to see the configuration options.
        // The settings file is located in the root of the CodeContext project.
        var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(SettingsPath));
        // check settings includes excluded paths, files, and extensions.
        if (settings.ExcludedPaths == null)
        {
            settings.ExcludedPaths = new string[] { };
        }
        if (settings.ExcludedFiles == null)
        {
            settings.ExcludedFiles = new string[] { };
        }
        if (settings.ExcludedExtensions == null)
        {
            settings.ExcludedExtensions = new string[] { };
        }
        // Create a list files in the project directory. Do not include files in the excluded paths or files or extensions.
        var files = new List<string>();
        foreach (var file in Directory.GetFiles(settings.ProjectPath, "*.*", SearchOption.AllDirectories))
        {
            if (settings.ExcludedPaths.Any(x => file.Contains(x)) ||
                settings.ExcludedFiles.Any(x => file.EndsWith(x)) ||
                settings.ExcludedExtensions.Any(x => file.EndsWith(x)))
            {
                continue;
            }
            else
            {
                files.Add(file);
            }
            
        }
        // check files found for excluded paths, files, and extensions and remove them.
        foreach (var file in files)
        {
            if (settings.ExcludedPaths.Any(x => file.Contains(x)) ||
                               settings.ExcludedFiles.Any(x => file.EndsWith(x)) ||
                                              settings.ExcludedExtensions.Any(x => file.EndsWith(x)))
            {
                files.Remove(file);
            }
        }

        Console.WriteLine($"Total files found: {files.Count}");
      
        Console.Clear();
        // Get functions, classes, and methods from the files.
        var context = new List<dynamic>();
        foreach (var file in files)
        {
            
            var fileContext = new ExpandoObject() as IDictionary<string, object>;
            fileContext.Add("FileName", file);
            fileContext.Add("Functions", new List<string>());
            fileContext.Add("Classes", new List<string>());
            fileContext.Add("Methods", new List<string>());
            var lines = File.ReadAllLines(file);
            foreach (var line in lines)
            {
                if (line.Contains("class "))
                {
                    var className = line.Split("class ")[1].Split(" ")[0];
                    ((List<string>)fileContext["Classes"]).Add(className);
                }
                if (line.Contains("function "))
                {
                    var functionName = line.Split("function ")[1].Split("(")[0];
                    ((List<string>)fileContext["Functions"]).Add(functionName);
                }
                if (line.Contains("void "))
                {
                    var methodName = line.Split("void ")[1].Split("(")[0];
                    ((List<string>)fileContext["Methods"]).Add(methodName);
                }
               
            }
            context.Add(fileContext);
        }
      
        // create UI for user to select files from the context file to deepen the context of.
        Console.Clear();
        Console.WriteLine("Select files to deepen the context of:");
        var selectedFiles = new List<string>();
        var pageIndex = 0;
        var pageSize = 20;
        var totalPages = (int)Math.Ceiling((double)context.Count / pageSize);
        while (true)
        {
            Console.Clear();
            for (var i = pageIndex * pageSize; i < (pageIndex * pageSize) + pageSize; i++)
            {
                if (i < context.Count)
                {// add a * to the selected files
                    if (selectedFiles.Contains(((IDictionary<string, object>)context[i])["FileName"]))
                    {
                        Console.WriteLine($"{i + 1}. *{((IDictionary<string, object>)context[i])["FileName"]}");
                    }
                    else
                    { 
                        Console.WriteLine($"{i + 1}. {((IDictionary<string, object>)context[i])["FileName"]}");
                    }
                }
            }
            Console.WriteLine("******************************************************************");
            // list the selected files
            Console.WriteLine("Selected Files:");
            foreach (var file in selectedFiles)
            {
                Console.WriteLine(file);
            }
            Console.WriteLine("N. Next Page");
            Console.WriteLine("P. Previous Page");
            Console.WriteLine("D. Done");
            var input = Console.ReadLine();
            if (input.ToLower() == "n")
            {
                pageIndex++;
                if (pageIndex >= totalPages)
                {
                    pageIndex = totalPages - 1;
                }
            }
            else if (input.ToLower() == "p")
            {
                pageIndex--;
                if (pageIndex < 0)
                {
                    pageIndex = 0;
                }
            }
            else if (input.ToLower() == "d")
            {
                break;
            }
            else
            {
                if (int.TryParse(input, out var fileIndex))
                {
                    if (fileIndex > 0 && fileIndex <= context.Count)
                    {
                        if (selectedFiles.Contains(((IDictionary<string, object>)context[fileIndex - 1])["FileName"]))
                        {
                            selectedFiles.Remove(((IDictionary<string, object>)context[fileIndex - 1])["FileName"].ToString());
                        }
                        else
                        {
                            selectedFiles.Add(((IDictionary<string, object>)context[fileIndex - 1])["FileName"].ToString());
                        }
                    }
                }
            }
        }
        // Get the functions, classes, and methods from the selected files.
        var deepContext = new List<dynamic>();
        // add entire file context to deep context
        foreach (var file in selectedFiles)
        {
            string[] strings = File.ReadAllLines(file);
            deepContext.Add(strings);

        }
        // Save the deep context to a file.
        string ContFile = JsonConvert.SerializeObject(context);
        string ContFile2 = JsonConvert.SerializeObject(deepContext);
        File.WriteAllText(settings.ContextFilePath, JsonConvert.SerializeObject(ContFile));
        // append the deep context to the context file
        File.AppendAllText(settings.ContextFilePath, JsonConvert.SerializeObject(ContFile2));





        
    }
}
