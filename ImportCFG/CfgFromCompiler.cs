using CfgCompLib.classes;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;



namespace CfgCompLib {
    public static class CfgFromCompiler {
        public static string ImportCompilerCfgRaw(string cFilePath) {       //function to start GCC as process and get cfg as string from process stdout 

            if (String.IsNullOrEmpty(cFilePath)) {
                throw new ArgumentNullException($"No path to a '*.c' - file provided to create CFG: {cFilePath}");
            }
           
            string compilerPath = Configuration.config.GetRequiredSection("Settings").GetValue<string>("CompilerDir");
            if (String.IsNullOrEmpty(compilerPath)) {
                compilerPath = Directory.GetCurrentDirectory(); 
            }
            
            string outputPath = Configuration.config.GetRequiredSection("Settings").GetValue<string>("OutputDir");
            if (String.IsNullOrEmpty(outputPath)) {
                outputPath = Directory.GetCurrentDirectory() + "\\outputs\\";
                if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);
            }

            string compilerOpt = Configuration.config.GetRequiredSection("Settings").GetValue<string>("CompilerOptimization");
            if (String.IsNullOrEmpty(compilerOpt)) {
                compilerOpt = "-O1";
            } 

            ProcessStartInfo psiGcc = new("gcc.exe") {      //config of the GCC process
                WorkingDirectory = compilerPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,              //output -> app
                RedirectStandardError = true,
                ArgumentList = {
                    $"{compilerOpt}",                       //compiler optimization
                    "-w",                                   //deactivate warnings
                    "-fno-builtin",                         //to prevent builtins like put replacing printf
                    "-fdump-tree-cfg=stdout",               //get the cfg from the output stream of process
                    $"{cFilePath}",
                    "-o",
                    $"{outputPath}\\compileDump.bin"        //path for -o needs to be separate arg
                }
            };

            Process procGcc = Process.Start(psiGcc);
            
            if (procGcc is null) {
                throw new InvalidOperationException("process could not be started to compile c-file internally");
            } else {
                string errors = procGcc.StandardError.ReadToEnd();
                if (errors.Length != 0) throw new OperationCanceledException($"gcc compilation ran on errors:\n------------------------------\n{errors}");

                return procGcc.StandardOutput.ReadToEnd();
            }
        }    
        public static Graph GenerateGraphFromRaw(string cfg) {  //create the internal cfg from compiler cfg string
            
            string line = "";
            string returnVal = "";
            int actBlockNum = 0;
            bool inFunctionBody = false;
            bool blockWithGoto = false;
            Dictionary<string, string> regexMatches = [];

            Graph graph = new();

            graph.AddNode(new Node(0, ["Start"]));  //start and end are not explicitly in the cfg, so create them according GCC standard  
            graph.AddNode(new Node(1, ["End"]));


            using (StringReader reader = new(cfg)) {
                while ((line = reader.ReadLine()) != null && !line.Contains("return")) {

                    //check if line contains a block declaration "<bb ...> : and get block number for actual block"
                    Match blockNum = Regex.Match(line, "<bb\\s+(\\d+)>\\s+:");
                    if (blockNum.Success) {
                        actBlockNum = int.Parse(blockNum.Groups[1].Value);
                        inFunctionBody = true;
                        blockWithGoto = false;
                        continue;
                    }

                    //add nodes according to compiler function analysis information (;;) until function body entered at 1st block declaration
                    if (!inFunctionBody) {
                        Match nodeNum = Regex.Match(line, ";;\\s+(\\d+)\\s+succs");
                        if (nodeNum.Success) {
                            Node node = new(int.Parse(nodeNum.Groups[1].Value));
                            graph.AddNode(node);
                        }
                        continue;
                    }

                    //check for selection and in case, split into separate node
                    if (line.Contains("if") && graph.GetNode(actBlockNum).GetLabel().Count != 0) {
                        int newNodeId = graph.GetHighestId()+1;
                        Node node = new(newNodeId);
                        graph.AddNode(node);
                        graph.AddEdge(graph.GetNode(actBlockNum), node);
                        actBlockNum = newNodeId;  
                    }

                    //check if line contains a jump declaration " goto <bb ...>; and get goto number (= succ node id)"
                    Match gotoNum = Regex.Match(line, "goto <bb\\s+(\\d+)>;");  
                    if (gotoNum.Success) {
                        int succId = int.Parse(gotoNum.Groups[1].Value);
                        graph.AddEdge(graph.GetNode(actBlockNum), graph.GetNode(succId));
                        blockWithGoto = true;
                        continue;
                    }
                    
                    // sequence block check and add edge based on actual block number + 1
                    if (line.Length == 0 && !blockWithGoto) {
                        graph.AddEdge(graph.GetNode(actBlockNum), graph.GetNode(actBlockNum+1));
                        continue;
                    }

                    if(line.Length != 0 && !line.Contains("else")) {

                        //check for SSA & splitted variables (e.g. "var.1_2" or "_1") and extract assigned value
                        Match varMerge = Regex.Match(line, "(\\w*\\.?\\d*_\\d+)\\s+=\\s+(.+);");              
                        if (varMerge.Success) {
                            regexMatches.Add(varMerge.Groups[1].Value, varMerge.Groups[2].Value);   
                            continue;
                        }
 
                        //replace the placeholder (e.g. "var.1_2 or _1) with the splitted variables value
                        foreach (var regexMatch in regexMatches.OrderByDescending(rm => rm.Key)) {           
                            line = Regex.Replace(line, regexMatch.Key, regexMatch.Value);
                        }

                        //get the value from the "D.01234 = (<type cast>) VALUE;" assignment
                        Match returnValue = Regex.Match(line, "D.\\d+\\s+=\\s+\\(?[^)]*\\)?\\s?(.+);");     
                        if (returnValue.Success) {           
                            returnVal = returnValue.Groups[1].Value;
                            continue;
                        }

                        //remove unnecessary control symbols  
                        line = line.Trim().Replace("if","").Replace("(","").Replace(")","").Replace(";", "").Replace("\"","'").Replace("\\n","").Replace(" ","");              
                        graph.GetNode(actBlockNum).AddExpression(line);          
                    }    
                }
                graph.AddEdge(graph.GetNode(0), graph.GetNode(2));
                graph.AddEdge(graph.GetNode(actBlockNum), graph.GetNode(1));
                graph.GetNode(actBlockNum).AddExpression("return " + returnVal);

                //remove nodes without label content 
                foreach (var node in graph.GetNodes().Values) {
                    if (node.GetLabel().Count == 0) {
                        graph.RemoveSequenceNode(node);
                    }
                }
            }
            return graph;
        }
    }
}


