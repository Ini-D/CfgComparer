using CfgCompLib;
using CfgCompLib.classes;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using static CfgCompLib.CfgFromCompiler;
using static CfgCompLib.CfgFromFlowChart;
using static CfgCompLib.GraphUtils;
using static CfgComparer.Settings;

class CfgComparer
{
    public static class Settings {

        internal static IConfiguration config = new ConfigurationBuilder().AddJsonFile("CfgComparerConfig.json").Build();
        public static string CFileDir { get; set; } = config.GetRequiredSection("Settings").GetValue<string>("cFileDir");
        public static string XmlFileDir { get; set; } = config.GetRequiredSection("Settings").GetValue<string>("xmlFileDir");
        public static string OutFileDir { get; set; } = config.GetRequiredSection("Settings").GetValue<string>("outFileDir");
    }

    static void Main(string[] args)
    {
        try {
            if (args.Contains("-help")) {
                Console.WriteLine("" +
                    "please provide following parameters:\n" +
                    "param 1: <c-program> \"*.c\"   file\n" +
                    "param 2: <flowchart> \"*.xml\" file\n" +
                    "param 3: <optional>  \"-p\" for printing out the graphs in *.dot format");
                return;
            }
            if (args.Length < 2) {
                throw new ArgumentException("please provide two input files (for help: \"-help\")");
            } else {
                if (!args[0].Contains(".c")) throw new ArgumentException("Please provide a *.c format file for param 1");
                if (!args[1].Contains(".xml")) throw new ArgumentException("Please provide a *.xml format file for param 2");
            }

            if (String.IsNullOrEmpty(CFileDir)) {
                CFileDir = Directory.GetCurrentDirectory() + "\\inputs\\";
                if (!Directory.Exists(CFileDir)) Directory.CreateDirectory(CFileDir);   
            }

            if (String.IsNullOrEmpty(XmlFileDir)) {
                XmlFileDir = Directory.GetCurrentDirectory() + "\\inputs\\";
                if (!Directory.Exists(XmlFileDir)) Directory.CreateDirectory(XmlFileDir);
            }

            if (String.IsNullOrEmpty(OutFileDir)) {
                OutFileDir = Directory.GetCurrentDirectory() + "\\outputs\\";
                if (!Directory.Exists(OutFileDir)) Directory.CreateDirectory(OutFileDir);   
            }

            string compilerCfg = ImportCompilerCfgRaw(CFileDir + args[0]);      //get the cfg string from GCC-Compiler

            List <Graph> graphs = [];
            
            Graph cfgGraph = ExpandToMaxGraph(GenerateGraphFromRaw(compilerCfg));   
            cfgGraph.Description = "Control Flow Graph";
            graphs.Add(cfgGraph);

            Graph fcGraph = ExpandToMaxGraph(GenerateGraphFromXML(XmlFileDir + args[1]));      
            fcGraph.Description = "Flow Chart";
            graphs.Add(fcGraph);

            foreach (Graph graph in graphs) {       
                Console.Write("Graph: " + graph.Description);
                foreach (var node in graph.GetNodes().Values) {     //graphs to console
                    Console.WriteLine("\n------------------------");
                    Console.Write(node.Id + "->");
                    node.GetSuccessors().ForEach(x => Console.Write($"[{x.Id}]"));

                    Console.WriteLine("\nOutdegree: " + node.OutDegree);

                    Console.Write(node.Id + "<-");
                    node.GetPredecessors().ForEach(x => Console.Write($"[{x.Id}]"));

                    Console.WriteLine("\nIndegree: " + node.InDegree);

                    Console.Write("[\"" + node.LabelToString() + "\"]");

                }

                if (args.Length >= 3) {
                    if(args[2].Equals("-p", StringComparison.CurrentCultureIgnoreCase)) {   
                        
                        ExportGraphToDot(graph, OutFileDir, graph.Description);     //if -p parameter set, export graphs
                    }  
                } 

                Console.WriteLine($"\n\n# nodes/edges in {graph.Description} : " + graph.NodeCount + " / " + graph.EdgeCount);
                Console.WriteLine("\n====================================================");
            };
            
            HashSet<(Node, Node)> mcs = FindMCCS(cfgGraph, fcGraph);    //calc MCCS for console

            Console.WriteLine("Node mapping in Maximum Common Connected Subgraph (MCCS):\n");
            if (mcs.Count == 0) {
                Console.WriteLine("<none>");
            } else {
                Console.WriteLine($"{"Node CFG",-10}|{"Node FC",-10}");
                foreach (var node in mcs) {
                    Console.WriteLine($"{node.Item1.Id,-10}|{node.Item2.Id,-10}");
                }
            }
            Console.WriteLine("\n====================================================");

            Console.WriteLine($"Found Label Matchings based on threshold {Configuration.EqualThreshold*100}% :\n");
            if (fcGraph.NodeCount == 0) {
                Console.WriteLine("<none>");
            } else {
                foreach (var nodeCfg in cfgGraph.GetNodes().Values) {
                    foreach (var nodeFc in fcGraph.GetNodes().Values) {
                        var (TotalEQ, _, _, _) = CalculateLabelEquality(nodeFc.GetLabel()[0], nodeCfg.GetLabel()[0]);       //compare all labels and give matchings to console
                        if (TotalEQ >= Configuration.EqualThreshold * 100) {
                            ;

                            Console.WriteLine($"Equality: {TotalEQ:000.00}% | [{nodeCfg.GetLabel()[0]}] <---> [{nodeFc.GetLabel()[0]}]");
                        }
                    }
                }  
            }
            Console.WriteLine("\n====================================================");

            HashSet<string> editSteps = [];

            var (totalCosts, splitCosts) = CalculateGED(cfgGraph, fcGraph, out editSteps);  //calculate GED based on internal calc MCCS 

            Console.WriteLine($"Edits made in flow chart to fit control flow graph:\n");

            if (editSteps.Count == 0) {
                Console.WriteLine("<none>");

            } else {
                foreach (var edit in editSteps) {
                    Console.WriteLine("- " + edit);
                }
            }
            Console.WriteLine($"\nCalculation of GED:\n\n" +
                $"Node add costs : {splitCosts.CostsNodeInsert}\n" +
                $"Node del costs : {splitCosts.CostsNodeDelete}\n" +
                $"Node rel costs : {splitCosts.CostsNodeRelabel}\n" +
                $"Edge add costs : {splitCosts.CostsEdgeInsert}\n" +
                $"Edge del costs : {splitCosts.CostsEdgeDelete}\n\n" +
                $"**TOTAL costs**: {totalCosts}\n" +
                "====================================================\n"
            );
            double maxPoints = 2 * cfgGraph.NodeCount + cfgGraph.EdgeCount;         //calculate evaluation
            double receivedPoints = maxPoints - totalCosts;

            Console.WriteLine("evaluation result: " + receivedPoints + "/" + maxPoints);
            Console.WriteLine("percentage: " + (receivedPoints * 100 / maxPoints).ToString("F2") + "%");

        } catch (Exception ex) { 
            Console.WriteLine(ex.Message);
        } 
    } 
}
