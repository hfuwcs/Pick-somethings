using System.Collections.Generic;

public class ElectricalNode
{
    public int Id { get; }
    public HashSet<Connector> Connectors { get; } = new HashSet<Connector>();

    public ElectricalNode(int id)
    {
        Id = id;
    }
}