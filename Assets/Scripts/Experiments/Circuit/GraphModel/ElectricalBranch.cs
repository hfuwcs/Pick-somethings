public class ElectricalBranch
{
    public CircuitComponent Component { get; }
    public ElectricalNode NodeA { get; }
    public ElectricalNode NodeB { get; }

    public ElectricalBranch(CircuitComponent component, ElectricalNode nodeA, ElectricalNode nodeB)
    {
        Component = component;
        NodeA = nodeA;
        NodeB = nodeB;
    }
}