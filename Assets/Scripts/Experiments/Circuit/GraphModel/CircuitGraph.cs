using System.Collections.Generic;

public class CircuitGraph
{
    public List<ElectricalNode> Nodes { get; } = new List<ElectricalNode>();
    public List<ElectricalBranch> Branches { get; } = new List<ElectricalBranch>();
}