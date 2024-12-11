using CfgCompLib.classes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Configuration;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using static CfgCompLib.Configuration;

namespace CfgCompLib {
    public static class Configuration {
        
        //configuration for the library - set in JSON file
        internal static IConfiguration config = new ConfigurationBuilder().AddJsonFile("CfgCompLibConfig.json").Build();
        public static double NodeInsertCost { get; set; } = config.GetRequiredSection("GraphEditCosts").GetValue<double>("NodeInsert");
        public static double NodeDeleteCost { get; set; } = config.GetRequiredSection("GraphEditCosts").GetValue<double>("NodeDelete");
        public static double NodeRelabelCost { get; set; } = config.GetRequiredSection("GraphEditCosts").GetValue<double>("NodeRelabel");
        public static double EdgeInsertCost { get; set; } = config.GetRequiredSection("GraphEditCosts").GetValue<double>("EdgeInsert");
        public static double EdgeDeleteCost { get; set; } = config.GetRequiredSection("GraphEditCosts").GetValue<double>("EdgeDelete");
        public static double AstQualtoQuantWeight { get; set; } = config.GetRequiredSection("LabelComparison").GetValue<double>("QualtoQuantWeight");
        public static double AstLiteralWeight { get; set; } = config.GetRequiredSection("LabelComparison").GetValue<double>("LiteralWeight");
        public static double EqualThreshold { get; set; } = config.GetRequiredSection("LabelComparison").GetValue<double>("EqualityThreshold"); 
    }
    public static class GraphUtils {
        
        public static void ExportGraphToDot(Graph g, string path, string fileName) {    //export graphs to a simple dot file
            using (StreamWriter outputFile = new(path + $"{fileName}.dot")) {
                outputFile.WriteLine("digraph{\n");

                foreach (var node in g.GetNodes().Values) {
                    outputFile.WriteLine(node.Id + $"[label=\"{node.Id}\n{node.LabelToString()}\"]");
                }

                foreach (var node in g.GetNodes().Values) {
                    node.GetSuccessors().ForEach(adjNode => outputFile.WriteLine(node.Id + "->" + adjNode.Id));
                }
                outputFile.WriteLine("}");
            }
        }
        public static Graph DeepCopy(Graph source) {    //to create copies of the graphs for various functions
            string xml = SerializeGraphToXml(source);
            Graph copy = DeserializeXmlToGraph(xml);
            return copy;
        }
        public static string SerializeGraphToXml(Graph graph) {
            DataContractSerializer serializer = new(typeof(Graph));
            StringBuilder builder = new();
            using (XmlWriter writer = XmlWriter.Create(builder)) {
                serializer.WriteObject(writer, graph);
            };
            return builder.ToString();
        }
        public static Graph DeserializeXmlToGraph(string xml) {
            Graph graph;
            DataContractSerializer deserializer = new(typeof(Graph));
            StringReader stringReader = new(xml);
            using (XmlReader reader = XmlReader.Create(stringReader)) {
                graph = (Graph)deserializer.ReadObject(reader);
            };
            return graph;
        }
        public static Graph ExpandToMaxGraph(Graph graph) {     //expand graphs to fit the rule -> one expression per node
            Graph expGraph = DeepCopy(graph);
            int maxId = expGraph.GetHighestId();
            foreach (var node in expGraph.GetNodes().Values) {
                Node expandedNode = node;
                while (node.GetLabel().Count > 1) {
                    expandedNode = new(++maxId, [node.GetExpressionAtRow(1)], [expandedNode], expandedNode.GetSuccessors());
                    node.RemoveExpression(1);
                    expGraph.AddSequenceNode(expandedNode);
                }
            }
            return expGraph;
        }
        public static Graph CompressToMinGraph(Graph graph) {   //shrink the graph to it's minimal size by merging sequenced nodes
            Graph compGraph = DeepCopy(graph);
            int maxId = compGraph.GetHighestId();
            foreach (var node in compGraph.GetNodes().Values) {
                if (node.InDegree != 0 && node.OutDegree == 1) {
                    Node succNode = compGraph.GetNode(node.GetSuccessors()[0].Id);
                    if (succNode == null) continue;
                    Node mergedNode = new(++maxId);
                    node.GetLabel().ForEach(expr => mergedNode.AddExpression(expr));
                    node.GetPredecessors().ForEach(predec => mergedNode.AddPredecessor(predec));
                    
                    while (succNode.OutDegree == 1) {
                        compGraph.RemoveSequenceNode(node);
                        succNode.GetLabel().ForEach(expr => mergedNode.AddExpression(expr));
                        if (mergedNode.GetSuccessors().Count != 0) {
                            mergedNode.RemoveSuccessor(mergedNode.GetSuccessors()[0]);
                            
                        }
                        mergedNode.AddSuccessor(succNode.GetSuccessors()[0]);
                        compGraph.RemoveSequenceNode(succNode);
                        succNode = compGraph.GetNode(mergedNode.GetSuccessors()[0].Id);
                    }   
                    mergedNode.GetPredecessors().ForEach(predec => compGraph.RemoveEdge(predec,succNode));
                    if (mergedNode.GetLabel().Count == node.GetLabel().Count) {
                        maxId--;
                        continue;
                    }
                    compGraph.AddNode(mergedNode);
                }
            }
            return compGraph;
        }
        public static Graph GenerateRandomCfg(int amountNodes) {    //random graph generator for testing functions

            Graph graph = new();
            Random random = new();

            for (int i = 0; i < amountNodes; i++) {
                char label = (char)random.Next(65, 91);     //create labels for the nodes, just one big char A to Z
                graph.AddNode(new(i, [label.ToString()]));
            }

            Node source = graph.GetNode(0);
            Node sink = graph.GetNode(1);
            int amountEdges;
            int maxOutdegree = 2;

            foreach (Node node in graph.GetNodes().Values) {
                if (node.Id == source.Id) {

                    amountEdges = random.Next(1, maxOutdegree + 1);
                    List<Node> successors = graph.GetNodes().Values.OrderBy(node => random.Next()).Take(amountEdges).ToList();

                    foreach (Node succ in successors) {
                        graph.AddEdge(source, succ);
                    }
                } else if (node.Id == sink.Id) {

                    List<Node> nodes = graph.GetNodes().Values.Where(n => n.Id != sink.Id && n.Id != source.Id && n.Id != node.Id).ToList();
                    amountEdges = random.Next(1, maxOutdegree + 1);
                    List<Node> predecessors = nodes.OrderBy(node => random.Next()).Take(amountEdges).ToList();

                    foreach (Node predec in predecessors) {
                        graph.AddEdge(predec, sink);
                    }
                } else {

                    List<Node> nodes = graph.GetNodes().Values.Where(n => n.Id != sink.Id && n.Id != source.Id && n.Id != node.Id).ToList();
                    amountEdges = random.Next(1, maxOutdegree + 1);

                    List<Node> successors = nodes.OrderBy(x => random.Next()).Take(amountEdges).ToList();   //order by randomly numbered nodes and take a sequence of nodes as succ
                    successors.ForEach(succ => graph.AddEdge(node, succ));

                    List<Node> predecessors = nodes.OrderBy(x => random.Next()).Take(amountEdges).ToList(); //order by randomly numbered nodes and take a sequence of nodes as predec
                    predecessors.ForEach(predec => graph.AddEdge(predec, node));
                }
            }
            return graph;
        }
        private static int DiffGraphPosition(Graph graph1, Graph graph2, Node node1, Node node2) {  //to calculate the difference in position of two nodes in their respective graphs (0-> same position)
            
            int totalDifference;
            var pathLength1 = graph1.ComputeShortestPaths(node1).Values.Sum();      
            var pathLength2 = graph2.ComputeShortestPaths(node2).Values.Sum(); 
            totalDifference = Math.Abs(pathLength1 - pathLength2);
         
            return totalDifference;
        }
        public static HashSet<(Node, Node)> FindMCCS(Graph cfg, Graph fc) {     //get the MCCS, global position of nodes after over-matching 
            var visitedEdges = new HashSet<((Node, Node), (Node, Node))>();
            var maxCCS = new HashSet<(Node, Node)>();
            
            //optimization: sorting makes sources visited earlier with a chance to prove complete isomorphism due full coverage
            var sortedFcNodes = fc.GetNodes().Values.OrderBy(node => node.InDegree).ThenByDescending(node => node.OutDegree).ToList();  

            foreach (var cfgNode in cfg.GetNodes().Values) {
                foreach (var fcNode in sortedFcNodes) {

                    var currentCCS = FindMCCS(cfgNode, fcNode, visitedEdges);

                    if (currentCCS.Count > cfg.NodeCount && currentCCS.Count > fc.NodeCount) {
                        foreach (var match in currentCCS) {
                            var diffPosition = DiffGraphPosition(cfg, fc, match.Item1, match.Item2);
                            if (diffPosition != 0) {
                                var cfgNodeInCCS = currentCCS.Where(pair => pair.Item1.Id == match.Item1.Id);     //get all matchings with this cfgNode inside at position (cfgNode,__)
                                var fcNodeInCCS  = currentCCS.Where(pair => pair.Item2.Id == match.Item2.Id);     //get all matchings with this fcNode inside at position (__,fcNode)

                                if (cfgNodeInCCS.Count() >= 2 && fcNodeInCCS.Count() >= 2) {  //prevent deletion if cfgNode and fcNode are only once in the mccs

                                    //delete the matching with the highest difference in graph position
                                    currentCCS.Remove(cfgNodeInCCS.Aggregate((x, y) => 
                                    DiffGraphPosition(cfg, fc, x.Item1, x.Item2) > DiffGraphPosition(cfg, fc, y.Item1, y.Item2) ? x : y));
                                }
                            };
                        }
                        return currentCCS;
                    } else if (currentCCS.Count == cfg.NodeCount && currentCCS.Count == fc.NodeCount) {
                        return currentCCS;
                    } else if (currentCCS.Count > maxCCS.Count) {
                        maxCCS = currentCCS;
                    }
                }
            }
            return maxCCS;
        }
        private static HashSet<(Node, Node)> FindMCCS(Node cfgNode, Node fcNode, HashSet<((Node, Node), (Node, Node))> visitedEdges) {
            var mapping = new HashSet<(Node, Node)>();  //becomes the common connected subgraph in one run

            if (!AreLocallyEqual(cfgNode, fcNode)) {    //check if neighbours and label are identically            
                return mapping;
            }
            mapping.Add((cfgNode, fcNode));

            foreach (var cfgSucc in cfgNode.GetSuccessors()) {
                foreach (var fcSucc in fcNode.GetSuccessors()) {
                    var edge = ((cfgNode, cfgSucc), (fcNode, fcSucc));

                    if (!visitedEdges.Contains(edge) && AreLocallyEqual(cfgSucc, fcSucc)) {
                        visitedEdges.Add(edge);
                        var subgraph = FindMCCS(cfgSucc, fcSucc, visitedEdges);
                        foreach (var nodePair in subgraph) {
                            mapping.Add(nodePair);
                        }
                    }
                }
            }
            return mapping;
        }
        public static Dictionary<(Node, Node), int> FindMCCSDeepCheck(Graph cfg, Graph fc) {    //get the MCCS, global position of nodes inside recursion 
            var visitedEdges = new HashSet<((Node, Node), (Node, Node))>();
            var maxCS = new Dictionary<(Node, Node), int>();
            var sortedFcNodes = fc.GetNodes().Values.OrderBy(node => node.InDegree).ThenByDescending(node => node.OutDegree).ToList();  //optimization: sorting makes Sources visited earlier with a chance to prove complete isomorphism due full coverage
            foreach (var cfgNode in cfg.GetNodes().Values) {
                foreach (var fcNode in sortedFcNodes) {

                    var currCS = FindMCCSDeepCheck(cfg, fc, cfgNode, fcNode, visitedEdges);

                    if (currCS.Count == cfg.NodeCount && currCS.Count == fc.NodeCount) {
                        return currCS;
                    } else if (currCS.Count > maxCS.Count) {
                        maxCS = currCS;
                    }
                }
            }
            return maxCS;
        }
        private static Dictionary<(Node cfgNode, Node fcNode),int> FindMCCSDeepCheck(Graph cfg, Graph fc,Node cfgNode, Node fcNode, HashSet<((Node, Node), (Node, Node))> visitedEdges) {
            var mapping = new Dictionary<(Node, Node),int>();

            if (!AreLocallyEqual(cfgNode, fcNode)) {
                return mapping;
            }
            
            mapping.Add((cfgNode, fcNode), DiffGraphPosition(cfg, fc, cfgNode, fcNode));    //check for the global position of the nodes and add to common connected subgraph
  
            foreach (var cfgSucc in cfgNode.GetSuccessors()) {
                foreach (var fcSucc in fcNode.GetSuccessors()) {
                    var edge = ((cfgNode, cfgSucc), (fcNode, fcSucc));

                    if (!visitedEdges.Contains(edge) && AreLocallyEqual(cfgSucc, fcSucc)) {
                        visitedEdges.Add(edge);
                        var subgraph = FindMCCSDeepCheck(cfg, fc, cfgSucc, fcSucc, visitedEdges);
                        foreach (var nodePair in subgraph) {
                            if (!mapping.ContainsKey(nodePair.Key)) {
                                mapping.Add(nodePair.Key, DiffGraphPosition(cfg, fc, nodePair.Key.cfgNode, nodePair.Key.fcNode));
                            } 
                        }
                    }
                }
            }
            return mapping;
        }
        private static bool AreLocallyEqual(Node node1, Node node2) {   //check two nodes if label and neighbourhood (predecs + succs) are equal
            if (!LabelIsEqual(node1,node2)) return false;
            if (node1.OutDegree != node2.OutDegree) return false;
            if (node1.InDegree != node2.InDegree) return false;

            foreach (var n1Succ in node1.GetSuccessors()) {
                bool found = false;
                foreach (var n2Succ in node2.GetSuccessors()) {
                    if (LabelIsEqual(n1Succ, n2Succ)) {
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
                
            }

            foreach (var n1Predec in node1.GetPredecessors()) {
                bool found = false;
                foreach (var n2Predec in node2.GetPredecessors()) {
                    if (LabelIsEqual(n1Predec, n2Predec)) {
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }
            return true;
        }
        private static Node NodeExistsInGraph(Graph graph, Node node) {
            foreach (var n in graph.GetNodes().Values) {
                if (LabelIsEqual(n, node))
                    return n;
            }
            return null;
        }
        private static bool EdgeExistsInGraph(Graph graph, Node source, Node target) {
            foreach (var node in graph.GetNodes().Values) {
                if (LabelIsEqual(node, source)) {
                    foreach (var successor in node.GetSuccessors()) {
                        if (LabelIsEqual(successor, target))
                            return true;
                    }
                }
            }
            return false;
        }
        private static Node FindMatchingNodeInGraph(Graph graph, Node node) {   //find a matching node in the graph based on neighbourhood not label
          
            foreach (var n in graph.GetNodes().Values) {
                
                if (n.OutDegree != node.OutDegree) continue;
                if (n.InDegree != node.InDegree) continue;

                bool successorsMatch = true;
                bool predecessorsMatch = true;

                foreach (var successor in n.GetSuccessors()) {
                    bool found = false;
                    foreach (var succ in node.GetSuccessors()) {
                        if (LabelIsEqual(successor, succ) || succ.OutDegree == successor.OutDegree && succ.InDegree == successor.InDegree) {
                            found = true;
                            break;
                        } 
                    }
                    if (!found) {
                        successorsMatch = false;
                        break;
                    } 
                }

                if (!successorsMatch) continue;

                foreach (var predecessor in n.GetPredecessors()) {
                    bool found = false;
                    foreach (var predec in node.GetPredecessors()) {
                        if(LabelIsEqual(predecessor, predec) || predec.OutDegree == predecessor.OutDegree && predec.InDegree == predecessor.InDegree) {
                            found = true;
                            break;
                        }
                    }
                    if (!found) {
                        predecessorsMatch = false;
                        break;
                    }
                }
                if (predecessorsMatch) return n;
            }
            return null;
        }
        public static (double totalCosts, (double CostsNodeInsert, double CostsNodeDelete, double CostsNodeRelabel, double CostsEdgeInsert, double CostsEdgeDelete) splitCosts) CalculateGED(Graph cfg, Graph fc, out HashSet<string> editSteps) {
            
            editSteps = [];

            var matches = FindMCCS(cfg, fc);
            
            if (matches.Count == cfg.NodeCount && matches.Count == fc.NodeCount) return (0, (0, 0, 0, 0, 0));    //MCCS consists of all elements in cfg and fc --> no edits required

            Graph restCfg = DeepCopy(cfg);
            Graph restFc = DeepCopy(fc);

            foreach (var nodePair in matches) {
                restCfg.RemoveNode(nodePair.Item1);
                restFc.RemoveNode(nodePair.Item2);
            }
            List<Node> processed = [];
            List<(Node source ,Node target)> relabelCandid = [];
            List<Node> insertCandid = [];

            double costsNodeInsert  = 0;
            double costsNodeDelete  = 0;
            double costsNodeRelabel = 0;
            double costsEdgeInsert  = 0;
            double costsEdgeDelete  = 0;
            
            foreach (var node in restCfg.GetNodes().Values) {
                var matchNode = NodeExistsInGraph(restFc, node);    //check if cfg node matches a fc node in label
                if (matchNode != null) {
                    foreach (var successor in node.GetSuccessors()) {
                        var matchSuccNode = NodeExistsInGraph(restFc, successor);   //check if cfg succ node matches a fc node in label
                        if (matchSuccNode != null) {
                            if(!EdgeExistsInGraph(restFc, node, successor)) {
                                costsEdgeInsert += EdgeInsertCost;
                                editSteps.Add($"Insert Edge from [\"{node.GetLabel()[0]}\"] to [\"{successor.GetLabel()[0]}\"] in flow chart");
                            }
                        }  
                    }
                    processed.Add(matchNode);   //mark fc node as processed to prevent other matchings
                    continue;
                }
  
                var matchingNode = FindMatchingNodeInGraph(restFc, node);   //find a node with same neighbourhood regardless label

                if (matchingNode == null) {
                    costsNodeInsert += NodeInsertCost;

                    if (editSteps.Contains($"Insert Node [\"{node.GetLabel()[0]}\"] into flow chart")) { //differentiate if nodes have same label  
                        editSteps.Add($"Insert Node [\"{node.GetLabel()[0]}\"]_ID_{node.Id} into flow chart");
                    } else {
                        editSteps.Add($"Insert Node [\"{node.GetLabel()[0]}\"] into flow chart");
                    }

                    foreach (var successor in node.GetSuccessors()) {
                        if (editSteps.Add($"Insert Edge from [\"{node.GetLabel()[0]}\"] to [\"{successor.GetLabel()[0]}\"] in flow chart")) {
                            costsEdgeInsert += EdgeInsertCost;
                        };
                    }
                    foreach (var predecessor in node.GetPredecessors()) {
                        if (editSteps.Add($"Insert Edge from [\"{predecessor.GetLabel()[0]}\"] to [\"{node.GetLabel()[0]}\"] in flow chart")) {
                            costsEdgeInsert += EdgeInsertCost;
                        };
                    }


                } else if (!LabelIsEqual(node, matchingNode)) {     //mark as candidate for relabeling

                    relabelCandid.Add((matchingNode,node));   
                }
            }

            foreach (var relCand in relabelCandid) {
                if (!processed.Contains(relCand.source)) {      //only relabel not processed fc nodes
                    costsNodeRelabel += NodeRelabelCost;
                    editSteps.Add($"Relabel Node [\"{relCand.source.GetLabel()[0]}\"] to [\"{relCand.target.GetLabel()[0]}\"] in flow chart");
                    relCand.source.RemoveExpression(0);
                    relCand.source.AddExpression(relCand.target.GetLabel()[0]);
                } else {
                    costsNodeInsert += NodeInsertCost;
                    editSteps.Add($"Insert Node [\"{relCand.target.GetLabel()[0]}\"] into flow chart");

                    foreach (var successor in relCand.target.GetSuccessors()) {
                        if (editSteps.Add($"Insert Edge from [\"{relCand.target.GetLabel()[0]}\"] to [\"{successor.GetLabel()[0]}\"] in flow chart")) {
                            costsEdgeInsert += EdgeInsertCost;
                        };
                    }
                    foreach (var predecessor in relCand.target.GetPredecessors()) {
                        if (editSteps.Add($"Insert Edge from [\"{predecessor.GetLabel()[0]}\"] to [\"{relCand.target.GetLabel()[0]}\"] in flow chart")) {
                            costsEdgeInsert += EdgeInsertCost;
                        };
                    }
                }

            }

            foreach (var node in restFc.GetNodes().Values) {    //cross check with fc nodes against cfg nodes
                var matchNode = NodeExistsInGraph(restCfg, node);
                if (matchNode != null) {
                    foreach (var successor in node.GetSuccessors()) {
                        var matchSuccNode = NodeExistsInGraph(restCfg, successor);
                        if (matchSuccNode != null) {
                            if (!EdgeExistsInGraph(restCfg, node, successor)) {
                                costsEdgeDelete += EdgeDeleteCost;
                                editSteps.Add($"Delete Edge from [\"{node.GetLabel()[0]}\"] to [\"{successor.GetLabel()[0]}\"] in flow chart");
                            }
                        }   
                    }
                    continue;
                }

                var matchingNode = FindMatchingNodeInGraph(restCfg, node);

                if (matchingNode == null) {

                    costsNodeDelete += NodeDeleteCost;
                    editSteps.Add($"Delete Node [\"{node.GetLabel()[0]}\"] from flow chart");

                } 
            }
            double totalCosts = costsNodeInsert + costsNodeDelete + costsNodeRelabel + costsEdgeInsert + costsEdgeDelete;
            editSteps = [.. editSteps.OrderDescending()];
            return (totalCosts, (costsNodeInsert, costsNodeDelete, costsNodeRelabel, costsEdgeInsert, costsEdgeDelete));
        }
        public static bool LabelIsEqual(Node node1, Node node2) {   //check nodes labels with created metric for equality

            var (TotalEQ, _, _, _) = CalculateLabelEquality(node1.GetLabel()[0],node2.GetLabel()[0]);    
            return TotalEQ >= (EqualThreshold * 100);
        }
        public static (double TotalEQ, double LiteralEQ, double SynEQ, double SemEQ) CalculateLabelEquality(string label1, string label2) {
            
            int stringDistance = CalculateStringDistance((label1.Trim().Replace(" ", "")), (label2.Trim().Replace(" ", "")));         //Remove all spaces,but case-sensitivity preserved (due to C being case-sensitive)

            double literalSimilarity = (1 - ((double)stringDistance / Math.Max(label1.Trim().Replace(" ", "").Length, label2.Trim().Replace(" ", "").Length))) * 100;      //Literal equation analysis

            var treeData1 = ExtractLabelTree(label1);
            var treeData2 = ExtractLabelTree(label2);

            int nodeCount1  = treeData1.Nodes.Count();
            int nodeCount2  = treeData2.Nodes.Count();
            int tokenCount1 = treeData1.Token.Count();
            int tokenCount2 = treeData2.Token.Count();

            double treeSynSimilarity =  ((double)(nodeCount1 + nodeCount2)   / (2 * Math.Max(nodeCount1, nodeCount2))   * 0.5 +
                                         (double)(tokenCount1 + tokenCount2) / (2 * Math.Max(tokenCount1, tokenCount2)) * 0.5) * 100;       //syntactic analysis

            var equalNodeKinds = treeData1.Nodes.Select(n1 => n1.Kind()).Intersect(treeData2.Nodes.Select(n2 => n2.Kind()));
            var totalNodeKinds = treeData1.Nodes.Select(n1 => n1.Kind()).Union(treeData2.Nodes.Select(n2 => n2.Kind()));

            var equalTokenKinds = treeData1.Token.Select(t1 => t1.Kind()).Intersect(treeData2.Token.Select(t2 => t2.Kind()));
            var totalTokenKinds = treeData1.Token.Select(t1 => t1.Kind()).Union(treeData2.Token.Select(t2 => t2.Kind()));

            double treeSemSimilarity = (((double)equalNodeKinds.Count() / totalNodeKinds.Count()) * 0.5 + ((double)equalTokenKinds.Count() / totalTokenKinds.Count()) * 0.5) * 100;     //semantic analysis

            double structuralSimilarity = treeSemSimilarity * AstQualtoQuantWeight + treeSynSimilarity * (1 - AstQualtoQuantWeight);

            double totalSimilarity = literalSimilarity * AstLiteralWeight + structuralSimilarity * (1 - AstLiteralWeight);

            return (totalSimilarity, literalSimilarity , treeSynSimilarity , treeSemSimilarity);

        }
        public static int CalculateStringDistance(string label1 = "", string label2 = "") {
            
            int[,] costMatrix = new int[label1.Length + 1, label2.Length + 1];

            for (int i = 0; i <= label1.Length; i++) { costMatrix[i, 0] = i; }  
            for (int j = 0; j <= label2.Length; j++) { costMatrix[0, j] = j; }
                
            for (int i = 1; i <= label1.Length; i++) {
                for (int j = 1; j <= label2.Length; j++) {
                    int replaceCost = (label1[i - 1] == label2[j - 1]) ? 0 : 1;     //if letter is equal -> no costs

                    costMatrix[i, j] = Math.Min(Math.Min(
                               costMatrix[i - 1, j] + 1,                   // delete cost
                               costMatrix[i, j - 1] + 1),                  // insert cost
                               costMatrix[i - 1, j - 1] + replaceCost);    // replace cost
                }
            }
            return costMatrix[label1.Length, label2.Length];
        }
        public static (IEnumerable<SyntaxNode> Nodes, IEnumerable<SyntaxToken> Token) ExtractLabelTree(string label) {      //get the syntax tree for a label

            var root = CSharpSyntaxTree.ParseText(label).GetRoot().DescendantNodes().First().DescendantNodes().First(); 

            return (root.DescendantNodesAndSelf(), root.DescendantTokens());

        }  
    }
}