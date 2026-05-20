using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;

namespace QSicon
{
    class QscriptConfig
    {
        public string CompilerExe;
        public string Version;
    }
    class QscriptProject
    {
        public string name;
        public string compile_path;
        public List<string> libs = new List<string>();
    }
    class QscriptInclude
    {
        public string name;
        public string url;

        public QscriptInclude(string name, string url)
        {
            this.name = name;
            this.url = url;
        }
    }

    internal class Program
    {
        private static string libs = "https://raw.githubusercontent.com/Qrell1/QsrLibs/refs/heads/main/libs.json";
        static List<QscriptInclude> ParseIncludes(ref JObject list, JToken inclusions)
        {
            List<JToken> inclusionsList = inclusions.ToList();
            List<QscriptInclude> resualt = new List<QscriptInclude>();

            foreach (var includeToken in inclusionsList)
            {
                string includeString = includeToken.ToString();
                string version = "last";

                if (includeString.Contains("="))
                {
                    string[] temp = includeString.Split('=');
                    version = temp.Last();
                    includeString = temp[0];
                }

                resualt.AddRange(ParseIncludes(ref list, list[includeString][version]["inclusions"]));
                resualt.Add(new QscriptInclude(includeToken.ToString(), list[includeString][version]["url"].ToString()));
            }

            return resualt;
        }

        static void Main(string[] args)
        {
            try
            {
                Console.Title = "qsr";
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.DefaultConnectionLimit = 10;
                ServicePointManager.Expect100Continue = false;
                bool outFlag = false;

                if (args[0] == "out") outFlag = true;
                if (args[0] == "update")
                {
                    string libsListPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    Console.WriteLine("Start Task Download...");
                    void getLibs()
                    {
                        using (WebClient wc = new WebClient())
                        {
                            //wc.Headers.Add("a", "a");
                            try
                            {
                                wc.DownloadFile(libs, $"{libsListPath}\\list.json");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                        }
                    }
                    for (int i = 0; i < 10; i++)
                    {
                        try { getLibs(); } catch { }
                    }
                    Console.WriteLine("End Update List Librares...");
                }
                else if (args[0] == "create")
                {
                    string project = args[1];
                    string config = File.ReadAllText("C:\\ProgramData\\Qscript\\config.json");
                    string path = Environment.CurrentDirectory + "\\" + project;
                    QscriptConfig qscriptConfig = JsonConvert.DeserializeObject<QscriptConfig>(config);
                    QscriptProject qscriptProject = new QscriptProject();

                    qscriptProject.name = project;
                    qscriptProject.compile_path = qscriptConfig.CompilerExe;

                    Directory.CreateDirectory(path);
                    Directory.CreateDirectory(path + "\\scr\\");

                    string strings = JsonConvert.SerializeObject(qscriptProject);
                    File.Create(path + "\\config.json").Close();
                    File.WriteAllText(path + "\\config.json", strings);
                    File.Create(path + "\\scr\\main.qs").Close();
                }
                else if (args[0] == "install" && args.Length == 3)
                {
                    string version = "last";

                    if (args[2].Contains("="))
                    {
                        string[] temp = args[2].Split('=');
                        version = temp.Last();
                        args[2] = temp[0];
                    }

                    string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string configPath = $"{Environment.CurrentDirectory}\\{args[1]}\\config.json";
                    Console.WriteLine(configPath);


                    string configStrings = File.ReadAllText(configPath);
                    QscriptProject qscriptProject = JsonConvert.DeserializeObject<QscriptProject>(configStrings);
                    string lib = args[2];
                    qscriptProject.libs.Add(lib);

                    string strings = JsonConvert.SerializeObject(qscriptProject);
                    File.WriteAllText(configPath, strings);

                    string libsListPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    strings = File.ReadAllText(libsListPath + "//list.json");

                    Directory.CreateDirectory($"{Environment.CurrentDirectory}\\{args[1]}\\scr\\{lib}\\");

                    JObject listObject = JObject.Parse(strings);
                    JToken libObject = listObject[lib];
                    /*
                     "qsrt" : {
                        "last" : "0.1",
                        "0.1" : {
                            "url" : "https://drive.usercontent.google.com/u/0/uc?id=1_u5CXdYjr_TwmzwsDpAmg8jbc6_5jARr&export=download",
                            "inclusions" : []
                        }
                      }
                     */
                    JToken versionObject = libObject[version];
                    if (version == "last")
                    {
                        versionObject = libObject[libObject[version].ToString()];
                    }

                    string libUrl = versionObject["url"].ToString();
                    string scr = $"{Environment.CurrentDirectory}\\{args[1]}\\scr\\";

                    Console.WriteLine(scr);                    
                    using (var client = new WebClient())
                    {
                        client.Headers.Add("a", "a");
                        List<QscriptInclude> inclusions = ParseIncludes(ref listObject, versionObject["inclusions"]);
                    
                        Console.WriteLine($"-={lib}\n--={libUrl}");
                        for (int i = 0; i < 10; i++)
                        {
                            try
                            {

                                Directory.CreateDirectory($"{scr}\\{lib}\\");
                                Console.WriteLine($"Start download {lib}...");
                                client.DownloadFile(libUrl, $"{scr}\\temp_{lib}");
                                Console.WriteLine("Download done!");
                                Console.WriteLine($"Start Unziping...");
                                ZipFile.ExtractToDirectory($"{scr}\\temp_{lib}", $"{scr}\\{lib}\\");
                                File.Delete($"{scr}\\temp_{lib}");
                                break;
                            }
                            catch { }
                        }

                        if (inclusions.Count != 0) Console.WriteLine("Installing inclusions");
                        foreach (var inc in inclusions)
                        {
                            libUrl = inc.url;
                            lib = inc.name;
                            Console.WriteLine($"-={inc.name}\n--={inc.url}");
                            for (int i = 0; i < 10; i++)
                            {
                                try
                                {

                                    Directory.CreateDirectory($"{scr}\\{lib}\\");
                                    Console.WriteLine($"Start download {lib}...");
                                    client.DownloadFile(libUrl, $"{scr}\\temp_{lib}");
                                    Console.WriteLine("Download done!");
                                    Console.WriteLine($"Start Unziping...");
                                    ZipFile.ExtractToDirectory($"{scr}\\temp_{lib}", $"{scr}\\{lib}\\");
                                    File.Delete($"{scr}\\temp_{lib}");
                                    return;
                                }
                                catch { }
                            }
                        }
                    }
                }
                else if (args[0] == "build" || outFlag)
                { // C:\Users\1\OneDrive\Рабочий стол\некоторые ярлыки\Документы\assembly\exampleruntimetesting\qs\bin\ex5
                    string projectPath = Environment.CurrentDirectory;
                    string configStrings = File.ReadAllText(projectPath + "\\config.json");
                    QscriptProject qscriptConfig = JsonConvert.DeserializeObject<QscriptProject>(configStrings);

                    var proc = new Process();
                    proc.StartInfo.FileName = qscriptConfig.compile_path;
                    proc.StartInfo.Domain = $"{projectPath}\\scr\\";
                    proc.StartInfo.Arguments = $"\"{projectPath}\\scr\\main.qs\" -program32";
                    proc.Start();

                    proc.WaitForExit();
                    proc.Close();
                }
                if (args[0] == "run" || outFlag)
                {
                    run:
                    string projectPath = Environment.CurrentDirectory;
                    string configStrings = File.ReadAllText(projectPath + "\\config.json");
                    QscriptProject qscriptConfig = JsonConvert.DeserializeObject<QscriptProject>(configStrings);

                    string compileExe = qscriptConfig.compile_path.Split('\\').Last();
                    string fasmExe = qscriptConfig.compile_path.Replace(compileExe, "") + "\\fasm\\FASM.EXE";

                    Directory.CreateDirectory($"{projectPath}\\out\\");

                    var proc = new Process();
                    proc.StartInfo.FileName = fasmExe;
                    proc.StartInfo.Domain = $"{projectPath}\\scr\\";
                    proc.StartInfo.Arguments = $"-m65536 \"{projectPath}\\scr\\bin\\main.qsr\" \"{projectPath}\\out\\main.exe\"";
                    proc.Start();

                    proc.WaitForExit();
                    proc.Close();

                    Console.Clear();
                    //Console.WriteLine($"{projectPath}\\out>start main.exe");
                    var outExe = new Process();
                    outExe.StartInfo.FileName = $"{projectPath}\\out\\main.exe";
                    outExe.StartInfo.Domain = $"{projectPath}\\out\\";
                    outExe.Start();
                }
                //Thread.Sleep(1000);
                return;
            }
            catch (Exception e) { Console.WriteLine(e.ToString()); Console.ReadLine(); }
        }
    }
}
