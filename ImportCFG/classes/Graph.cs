using System.Runtime.Serialization;

namespace CfgCompLib.classes {

    [DataContract]
    public class Graph {

        [DataMember(Order = 1)]
        public string Description { get; set; } = "";

        [DataMember(Order = 2)]
        public int NodeCount { get; private set; }

        [DataMember(Order = 3)]
        public int EdgeCount { get; private set; }

        [DataMember(Order = 4)]
        private Dictionary<int, Node> AdjacencyList = [];
        
        public bool AddNode(Node node) {
            if (AdjacencyList.ContainsKey(node.Id)) return false;
            AdjacencyList.Add(node.Id, node);

            node.GetPredecessors().ForEach(predec => AddEdge(predec, node));
            node.GetSuccessors().ForEach(succ => AddEdge(node, succ));

            NodeCount++;
            return true;
        }
        public bool AddSequenceNode(Node node) {    //add a node into the existng graph by adjusting the predec and succ after insert
            if (AdjacencyList.ContainsKey(node.Id)) return false;
            AdjacencyList.Add(node.Id, node);

            node.GetPredecessors().ForEach(predec => AddEdge(predec, node));
            node.GetSuccessors().ForEach(succ => AddEdge(node, succ));

            foreach (var succ in node.GetSuccessors()) {
                foreach (var predec in node.GetPredecessors()) {
                    if (AdjacencyList.ContainsKey(predec.Id) && AdjacencyList.ContainsKey(succ.Id)) {
                        RemoveEdge(predec, succ);
                    }
                }
            }
            NodeCount++;
            return true;
        }
        public bool RemoveNode(Node node) {
            if (!AdjacencyList.ContainsKey(node.Id)) return false;
            
            node.GetPredecessors().ForEach(predec => RemoveEdge(predec, node));
            node.GetSuccessors().ForEach(succ => RemoveEdge(node, succ));
            AdjacencyList.Remove(node.Id);
            NodeCount--;
            return true;
        }
        public bool RemoveSequenceNode(Node node) {     //remove node, but preserve graph connection (only poss. if node is in sequence)
            if (!AdjacencyList.ContainsKey(node.Id)) return false;
            if (node.OutDegree > 1) return false;

            foreach (var predec in node.GetPredecessors()) {
                RemoveEdge(predec,node);
                AddEdge(predec, node.GetSuccessors()[0]);
            };
            RemoveEdge(node, node.GetSuccessors()[0]);
            AdjacencyList.Remove(node.Id);
            NodeCount--;
            return true;
        }
        public Node GetNode(int id) {
            if(AdjacencyList.TryGetValue(id, out Node value)) return value;
            return null;
        }
        public Dictionary<int, Node> GetNodes() => new(AdjacencyList);
        public bool AddEdge(Node source, Node target) {
            if (AdjacencyList.ContainsKey(source.Id) && AdjacencyList.ContainsKey(target.Id)) {
                
                AdjacencyList[source.Id].AddSuccessor(target);
                AdjacencyList[target.Id].AddPredecessor(source);
                EdgeCount++;
                return true;

            } else {
                if (AdjacencyList.ContainsKey(source.Id)) {
                    AdjacencyList[source.Id].RemoveSuccessor(target);
                } else {
                    AdjacencyList[target.Id].RemovePredecessor(source);
                }
            }
            return false;   
        }
        public bool RemoveEdge(Node source, Node target) {
            if (AdjacencyList.ContainsKey(source.Id) && AdjacencyList.ContainsKey(target.Id)) {
                if(AdjacencyList[source.Id].RemoveSuccessor(target) && AdjacencyList[target.Id].RemovePredecessor(source)) {
                    EdgeCount--;
                }
                return true;
            }
            return false;      
        }
        public int GetHighestId() {
            if(AdjacencyList.Count != 0) return AdjacencyList.Keys.Max();
            return 0;
        }
        public Dictionary<Node, int> ComputeShortestPaths(Node startNode) { //bfs traversal and add one level-wise

            var queue = new Queue<Node>();
            var visited = new HashSet<Node>();
            var distances = new Dictionary<Node, int>();

            foreach (var node in AdjacencyList.Values) {
                distances.Add(node, 1000);  //initial dist = 1000, due to summation in comparison check, to prevent integer overflow
            }
            queue.Enqueue((startNode));
            visited.Add(startNode);
            distances[startNode] = 0;

            while (queue.Count > 0) {

                var currentNode = queue.Dequeue();
               
                foreach (var succ in currentNode.GetSuccessors()) {

                    if (visited.Contains(succ)) continue;
                    visited.Add(succ);
                    distances[succ] = distances[currentNode] + 1;
                    queue.Enqueue(succ);
                }
            }    
            return distances;
        }
    }
}