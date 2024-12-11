using System.Runtime.Serialization;

namespace CfgCompLib.classes;


[DataContractAttribute(IsReference = true)]
public class Node : IComparable<Node> {     //IComparable just to allow internal ordering

    [DataMember(Order = 1)]
    public int Id { get; set; }

    [DataMember(Order = 2)]
    public int InDegree { get; private set; }

    [DataMember(Order = 3)]
    public int OutDegree { get; private set; }

    [DataMember(Order = 4)]
    private readonly List<string> Label = [];

    [DataMember(Order = 5)]
    
    private List<Node> Predecessors = [];

    [DataMember(Order = 6)]
    private List<Node> Successors = [];

    public Node(int id, List<string> label = null, List<Node> predecessors = null, List<Node> successors = null) {
        Id = id;
        if (label != null) Label = label;
        predecessors?.ForEach(predec => AddPredecessor(predec));
        successors?.ForEach(succs => AddSuccessor(succs));
    }
    public Node GetSuccessor(int id) {
        Node foundNode = Successors.Find(succ => succ.Id == id);
        
        return new(foundNode.Id, foundNode.GetLabel(), foundNode.GetPredecessors(), foundNode.GetSuccessors());
    }
    public bool AddSuccessor(Node succ) {
        if (Successors != null) {
            if (Successors.Contains(succ)) return false;
            Successors.Add(succ);
            OutDegree++;
            return true;
        }
        return false;
    }
    public bool RemoveSuccessor(Node succ) {
        if (Successors != null) {
            if (!Successors.Contains(succ)) return false;
            Successors.Remove(succ);
            OutDegree--;
            return true;
        }
        return false;
    }
    public List<Node> GetSuccessors() {
        if (Successors != null) {
            return new(Successors);
        }
        return null;
    }
    public bool SetSuccessors(List<Node> successors) {
        if (successors == null) return false;
        Successors = successors;
        OutDegree = successors.Count;

        return true;
    }
    public Node GetPredecessor(int id) {
        Node foundNode = Predecessors.Find(predec => predec.Id == id);

        return new(foundNode.Id, foundNode.GetLabel(), foundNode.GetPredecessors(), foundNode.GetSuccessors());
    }
    public bool AddPredecessor(Node predec) {
        if (Predecessors != null) {
            if (Predecessors.Contains(predec)) return false;
            Predecessors.Add(predec);
            InDegree++;
            return true;
        }
        return false;
    }
    public bool RemovePredecessor(Node predec) {
        if (Predecessors != null) {
            if (!Predecessors.Contains(predec)) return false;
            Predecessors.Remove(predec);
            InDegree--;
            return true;
        }
        return false;
    }
    public List<Node> GetPredecessors() {
        if (Predecessors != null) {
            return new(Predecessors);
        }
        return null;
    }
    public bool SetPredecessors(List<Node> predecessors) {
        if (predecessors == null) return false;
        Predecessors = predecessors;
        InDegree = predecessors.Count;
        return true;
    }
    public List<string> GetLabel() {
        if (Label != null) {
            return new(Label);
        }
        return null;
    }
    public string LabelToString() {
        if (Label != null) {
            return String.Join("\n", Label);
        }
        return "";      
    }
    public void AddExpression(string exp) => Label?.Add(exp);
    public bool RemoveExpression(int expPosition) {
        if (Label != null) {
            Label.RemoveAt(expPosition);
            return true;
        }
        return false;

    }
    public string GetExpressionAtRow(int row) {
        if (Label != null) {
            return Label[row];
        }
        return "";
    }
    public int CompareTo(Node other) {  //just used for internal ordering, no graph compare
        
        return Id.CompareTo(other.Id);
    }  
}

