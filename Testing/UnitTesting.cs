using CfgCompLib.classes;
using System.Diagnostics;
using Xunit.Abstractions;
using static CfgCompLib.GraphUtils;

namespace CfgComparerTesting {
    public class UnitTesting {

        private readonly Graph _cfg = new();

        private readonly Graph _fc = new();

        private readonly ITestOutputHelper output;

        public UnitTesting(ITestOutputHelper output) {
            this.output = output;

            Graph cfg = new Graph();
            cfg.AddNode(new(0, ["Start"]));
            cfg.AddNode(new(1, ["End"]));
            cfg.AddNode(new(2, ["y!=0"]));         
            cfg.AddNode(new(3, ["x>y"]));           
            cfg.AddNode(new(4, ["x=x-y"]));         
            cfg.AddNode(new(5, ["y=y-x"]));        
            cfg.AddNode(new(6, ["x!=y"]));          
            cfg.AddNode(new(7, ["return x"]));

            cfg.AddEdge(cfg.GetNode(0), cfg.GetNode(2));
            cfg.AddEdge(cfg.GetNode(2), cfg.GetNode(6));
            cfg.AddEdge(cfg.GetNode(2), cfg.GetNode(7));
            cfg.AddEdge(cfg.GetNode(3), cfg.GetNode(4));
            cfg.AddEdge(cfg.GetNode(3), cfg.GetNode(5));
            cfg.AddEdge(cfg.GetNode(4), cfg.GetNode(6));
            cfg.AddEdge(cfg.GetNode(5), cfg.GetNode(6));
            cfg.AddEdge(cfg.GetNode(6), cfg.GetNode(3));
            cfg.AddEdge(cfg.GetNode(6), cfg.GetNode(7));
            cfg.AddEdge(cfg.GetNode(7), cfg.GetNode(1));
            _cfg = cfg;

            Graph fc = new Graph();
            fc.AddNode(new(0, ["Start"]));
            fc.AddNode(new(1, ["End"]));
            fc.AddNode(new(2, ["y!=0"]));           
            fc.AddNode(new(3, ["x!=y"]));           
            fc.AddNode(new(4, ["return x"]));
            fc.AddNode(new(5, ["x>y"]));            
            fc.AddNode(new(6, ["x=x-y"]));          
            fc.AddNode(new(7, ["y=y-x"]));          

            fc.AddEdge(fc.GetNode(0), fc.GetNode(2));
            fc.AddEdge(fc.GetNode(2), fc.GetNode(3));
            fc.AddEdge(fc.GetNode(2), fc.GetNode(4));
            fc.AddEdge(fc.GetNode(3), fc.GetNode(5));
            fc.AddEdge(fc.GetNode(3), fc.GetNode(4));
            fc.AddEdge(fc.GetNode(5), fc.GetNode(6));
            fc.AddEdge(fc.GetNode(5), fc.GetNode(7));
            fc.AddEdge(fc.GetNode(6), fc.GetNode(3));
            fc.AddEdge(fc.GetNode(7), fc.GetNode(3));
            fc.AddEdge(fc.GetNode(4), fc.GetNode(1));
            _fc = fc;

        }
        [Fact]
        public void Statistics_MCCS_EqualGraphs() { //test for under- or over-matching of isomorphic graphs

            TimeSpan total = new();
            int runs = 100;
            int graphSize = 100;
            int maxedMCS = 0;
            int decimatedMCS = 0;
            int exceededMCS = 0;

            for (int i = 0; i < runs; i++) {
                Graph rnd = GenerateRandomCfg(graphSize);
                Graph rnd2 = DeepCopy(rnd);
                Stopwatch sw = Stopwatch.StartNew();
                var mcs = FindMCCS(rnd, rnd2);
                sw.Stop();
                total += sw.Elapsed;

                if (mcs.Count != graphSize) {
                    if (mcs.Count > graphSize) exceededMCS++;
                    if (mcs.Count < graphSize) decimatedMCS++;
                } else {
                    maxedMCS++;
                }
            }
            output.WriteLine("mean runtime: " + (total / runs));
            output.WriteLine("maxed MCCS count: " + maxedMCS);
            output.WriteLine("decimated MCCS count: " + decimatedMCS);
            output.WriteLine("exeeded MCCS count: " + exceededMCS);
            output.WriteLine("percentage of MCCS misses: " + (exceededMCS + decimatedMCS) * 100 / (double)runs + "%");
        }
        [Fact]
        public void Statistics_MCCS_vs_MCCSDeep() { //test for comparing the runtime of the two insertion points for global position 

            double totalMccs = 0;
            double totalMccsDeep = 0;

            List<(int,double,int,double)> results = [];
            int runs = 50;
            int graphSize = 100;

            for (int i = 1; i <= runs; i++) {
                Graph rnd = GenerateRandomCfg(graphSize);
                Graph rnd2 = DeepCopy(rnd);
                Stopwatch sw = Stopwatch.StartNew();
                var mccs = FindMCCS(rnd, rnd2);
                sw.Stop();
                var mccsCount = mccs.Count;
                var mccsTime = sw.Elapsed.TotalSeconds;
                totalMccs += mccsTime;


                sw.Restart();
                var mccsDeep = FindMCCSDeepCheck(rnd, rnd2);
                sw.Stop();

                var mccsDeepCount = mccsDeep.Count;
                var mccsDeepTime = sw.Elapsed.TotalSeconds;
                totalMccsDeep += mccsDeepTime;

                results.Add((mccsCount, mccsTime, mccsDeepCount, mccsDeepTime));
            }
            output.WriteLine(" run | mccs # | mccs runtime (s) | mccsDeep # | mccsDeep runtime (s)");
            for (int i = 0; i < results.Count; i++) {
               
                output.WriteLine(
                    $"{(i + 1).ToString("D4")} | {results[i].Item1.ToString("D4")} | {results[i].Item2.ToString("F2")} | {results[i].Item3.ToString("D4")} | {results[i].Item4.ToString("F2")} "
                );
            }
            output.WriteLine($"mccs mean time: {(totalMccs/runs).ToString("F2")} | mccsDeep mean time: {(totalMccsDeep/runs).ToString("F2")}");
        }
        [Fact]
        public void MCCS_CompleteFlowChart() {  //test with cfg and fc being complete
            Graph fc = DeepCopy(_fc);
            HashSet<(Node, Node)> mcs = [];
            mcs = FindMCCS(_cfg, fc);
            Assert.Equal(8, mcs.Count);
            
            Assert.Contains((_cfg.GetNode(0), fc.GetNode(0)), mcs);
            Assert.Contains((_cfg.GetNode(2), fc.GetNode(2)), mcs);
            Assert.Contains((_cfg.GetNode(6), fc.GetNode(3)), mcs);
            Assert.Contains((_cfg.GetNode(3), fc.GetNode(5)), mcs);
            Assert.Contains((_cfg.GetNode(4), fc.GetNode(6)), mcs);
            Assert.Contains((_cfg.GetNode(5), fc.GetNode(7)), mcs);
            Assert.Contains((_cfg.GetNode(7), fc.GetNode(4)), mcs);
            Assert.Contains((_cfg.GetNode(1), fc.GetNode(1)), mcs);   
        }
        [Fact]
        public void MCCS_IncompleteFlowChartWith2Terminals() {  //test with one edge missing in the fc
            Graph fc = DeepCopy(_fc);
            HashSet<(Node, Node)> mcs = [];
            fc.RemoveEdge(fc.GetNode(3), fc.GetNode(4));
            
            mcs = FindMCCS(_cfg, fc);
            Assert.Equal(3, mcs.Count);

            Assert.Contains((_cfg.GetNode(3), fc.GetNode(5)), mcs); // MCCS 1
            Assert.Contains((_cfg.GetNode(4), fc.GetNode(6)), mcs); // MCCS 1
            Assert.Contains((_cfg.GetNode(5), fc.GetNode(7)), mcs); // MCCS 1

            //Assert.Contains((7, 4), mcs);   //not part of the MCCS (though reachable from successors, but in-degree not ok)
            //Assert.Contains((6, 3), mcs);   //not part of the MCCS (though reachable from predecessors, but out-degree not ok)
        }
        [Fact]
        public void MCCS_IncompleteFlowChartWithMoreTerminals() {   //test with disconnected "Start" node and "End" node -- so 2 sources and 2 sinks
            Graph fcWoStart = DeepCopy(_fc);
            Graph fcWoEnd = DeepCopy(_fc);
            HashSet<(Node, Node)> mcsWoStart = [];
            HashSet<(Node, Node)> mcsWoEnd = [];
            fcWoStart.RemoveEdge(fcWoStart.GetNode(0), fcWoStart.GetNode(2));
            fcWoEnd.RemoveEdge(fcWoEnd.GetNode(4), fcWoEnd.GetNode(1));

            mcsWoStart = FindMCCS(_cfg, fcWoStart);
            mcsWoEnd = FindMCCS(_cfg, fcWoEnd);
            Assert.Equal(6, mcsWoStart.Count);

            //Assert.Contains((2, 2), mcsWoStart);  //not part of the MCS (though reachable from predecessors, but in-degree not ok)
            Assert.Contains((_cfg.GetNode(6), fcWoStart.GetNode(3)), mcsWoStart);
            Assert.Contains((_cfg.GetNode(3), fcWoStart.GetNode(5)), mcsWoStart);
            Assert.Contains((_cfg.GetNode(4), fcWoStart.GetNode(6)), mcsWoStart);
            Assert.Contains((_cfg.GetNode(5), fcWoStart.GetNode(7)), mcsWoStart);
            Assert.Contains((_cfg.GetNode(7), fcWoStart.GetNode(4)), mcsWoStart);
            Assert.Contains((_cfg.GetNode(1), fcWoStart.GetNode(1)), mcsWoStart);

            Assert.Equal(6, mcsWoEnd.Count);

            Assert.Contains((_cfg.GetNode(0), fcWoEnd.GetNode(0)), mcsWoEnd);
            Assert.Contains((_cfg.GetNode(2), fcWoEnd.GetNode(2)), mcsWoEnd);
            Assert.Contains((_cfg.GetNode(6), fcWoEnd.GetNode(3)), mcsWoEnd);
            Assert.Contains((_cfg.GetNode(3), fcWoEnd.GetNode(5)), mcsWoEnd);
            Assert.Contains((_cfg.GetNode(4), fcWoEnd.GetNode(6)), mcsWoEnd);
            Assert.Contains((_cfg.GetNode(5), fcWoEnd.GetNode(7)), mcsWoEnd);
            //Assert.Contains((7, 4), mcsWoEnd);    //not part of the MCS (though reachable from predecessors, but out-degree not ok)
        }
        [Fact]
        public void GED_EqualGraphs() { //test of GED with equal graphs
            HashSet<string> edits = [];
            var costs = CalculateGED(_cfg, _fc,out edits);

            edits.ToList().ForEach(ed => output.WriteLine(ed));
            output.WriteLine("Total costs: "+ costs.totalCosts.ToString());

            Assert.Equal(0, costs.splitCosts.CostsNodeInsert);
            Assert.Equal(0, costs.splitCosts.CostsNodeDelete);
            Assert.Equal(0, costs.splitCosts.CostsNodeRelabel);
            Assert.Equal(0, costs.splitCosts.CostsEdgeInsert);
            Assert.Equal(0, costs.splitCosts.CostsEdgeDelete);
            Assert.Equal(0, costs.totalCosts);
        }
        [Fact]
        public void GED_FcEmpty() {     //test of GED with empty fc
            HashSet<string> edits = [];
            Graph emptyGraph = new();
            var costs = CalculateGED(_cfg, emptyGraph, out edits);

            edits.ToList().ForEach(ed => output.WriteLine(ed));
            output.WriteLine("Total costs: " + costs.totalCosts.ToString());

            Assert.Equal(16, costs.splitCosts.CostsNodeInsert);
            Assert.Equal(0, costs.splitCosts.CostsNodeDelete);
            Assert.Equal(0, costs.splitCosts.CostsNodeRelabel);
            Assert.Equal(10, costs.splitCosts.CostsEdgeInsert);
            Assert.Equal(0, costs.splitCosts.CostsEdgeDelete);
            Assert.Equal(26, costs.totalCosts);
        }
        [Fact]
        public void GED_NodeDiff() {    //test of GED with node difference
            HashSet<string> edits = [];

            Graph fc = DeepCopy(_fc);

            fc.RemoveNode(fc.GetNode(0));

            var costs = CalculateGED(_cfg, fc, out edits);

            edits.ToList().ForEach(ed => output.WriteLine(ed));
            output.WriteLine("Total costs: " + costs.totalCosts.ToString());

            Assert.Equal(2, costs.splitCosts.CostsNodeInsert);
            Assert.Equal(0, costs.splitCosts.CostsNodeDelete);
            Assert.Equal(0, costs.splitCosts.CostsNodeRelabel);
            Assert.Equal(1, costs.splitCosts.CostsEdgeInsert);
            Assert.Equal(0, costs.splitCosts.CostsEdgeDelete);
            Assert.Equal(3, costs.totalCosts);
        }
        [Fact]
        public void GED_LabelDiff() {   //test of GED with label difference
            HashSet<string> edits = [];
            Graph fc = DeepCopy(_fc);

            fc.GetNode(5).RemoveExpression(0);
            fc.GetNode(5).AddExpression("Test");

            var costs = CalculateGED(_cfg, fc, out edits);

            edits.ToList().ForEach(ed => output.WriteLine(ed));
            output.WriteLine("Total costs: " + costs.totalCosts.ToString());

            Assert.Equal(0, costs.splitCosts.CostsNodeInsert);
            Assert.Equal(0, costs.splitCosts.CostsNodeDelete);
            Assert.Equal(1, costs.splitCosts.CostsNodeRelabel);
            Assert.Equal(0, costs.splitCosts.CostsEdgeInsert);
            Assert.Equal(0, costs.splitCosts.CostsEdgeDelete);
            Assert.Equal(1, costs.totalCosts);
        }
        [Fact]
        public void GED_EdgeDiff() {    //test of GED with edge difference
            HashSet<string> edits = [];
            Graph fc = DeepCopy(_fc);
            fc.RemoveEdge(fc.GetNode(3), fc.GetNode(4));

            var costs = CalculateGED(_cfg, fc, out edits);

            edits.ToList().ForEach(ed => output.WriteLine(ed));
            output.WriteLine("Total costs: " + costs.totalCosts.ToString());

            Assert.Equal(0, costs.splitCosts.CostsNodeInsert);
            Assert.Equal(0, costs.splitCosts.CostsNodeDelete);
            Assert.Equal(0, costs.splitCosts.CostsNodeRelabel);
            Assert.Equal(1, costs.splitCosts.CostsEdgeInsert);
            Assert.Equal(0, costs.splitCosts.CostsEdgeDelete);
            Assert.Equal(1, costs.totalCosts);
        }
        [Fact]
        public void Label_Equality() {  //test for various label inputs
            List<(string, string)> graphLabels = [
                //("S t art", "    Start"),
                //("End", "Ende   "),
                //("Anfang","Start"),
                //(" a = 10", "int a = 10"),
                //("a > 20", "if(a > 20)"),
                //("f <= x", "f < x"),
                //("d = (a+b)*x", "d = (a*b)+x"),
                //("d = 3*((a*b)+x)","d = a*b"),
                //("if(a == b)","if(a = b)"),
                //("a = x+/10 "," a= x+10"),
                //("a = a+1", "a++"),
                //("a = a * 3", "a *= 3"),
                //("printf(\"Hallo, Welt\")", "\"Hallo, Welt\""),
                //("a = b * c", "e = f * g"),
                //("Start","End"),
                //("End", "return x"),
                //("y!=0","x>y"),
                //("x>y","x!=y"),
                //("x=x-y","y=y-x"),
                //("y=y-x","x=x-y"),
                //("x!=y","y!=0"),
                //("return x","Start"),

                ("a = 19", "Anzahl = 9"),           
                ("return","Wurst"),                 
                ("if 1","Wenn(1)"),                 
                ("a = 19", "Anzahl= 19"),           
                ("x=x-y","y=y-x"),                  
                ("a = 19", "a <= 19"),              
                ("a[8] = 10","a(8) = 10"),          
            ];

            output.WriteLine("Equality Measurements:");

            foreach (var labelPair in graphLabels) {
                var calc = CalculateLabelEquality(labelPair.Item1, labelPair.Item2);

                output.WriteLine($"Total: {calc.TotalEQ:000.00} Literal: {calc.LiteralEQ:000.00} Syn: {calc.SynEQ:000.00} Sem: {calc.SemEQ:000.00}  -- [{labelPair.Item1}] <> [{labelPair.Item2}]");
            }
        }
        [Fact]
        public void Label_CalcStringDistance() {    //test of string distance function
           
            string string1 = "y=y-x";
            string string2 = "x=x-y";

            var dist = CalculateStringDistance(string1, string2);

            output.WriteLine("String Distance:");

            output.WriteLine(dist.ToString());

            Assert.Equal(3, dist);
        }
    }
}