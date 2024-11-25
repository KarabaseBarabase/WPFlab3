using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace WPFlab3
{
    public class Node : INotifyPropertyChanged
    {
        private int _myValue;

        public int MyValue
        {
            get { return _myValue; }
            set
            {
                if (_myValue != value)
                {
                    _myValue = value;
                    OnPropertyChanged(nameof(Node.MyValue));
                }
            }
        }
        public Point position;
        public NodePicture nodePic;
        public HashSet<Edge> edges = new HashSet<Edge>(); //список ребер
        public Dictionary<Node, Edge> parents = new Dictionary<Node, Edge>(); //список родителей 
        public Node(int value) { MyValue = value; }
        public Node(int value, Point pos) { MyValue = value; position = pos; }
        public Node(int value, Point pos, NodePicture nodePicture) { MyValue = value; position = pos; nodePic = nodePicture; }
        public Node() { }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public string ToString() { return _myValue.ToString(); }
        public Node AddOrGetNode(Dictionary<int, Node> graph, int value)
        {
            if (value == -1) return null;
            if (graph.ContainsKey(value))
                return graph[value];
            Node node = new Node(value);
            graph.Add(value, node);
            return node;
        }
        //public Dictionary<int, Node> CreateGraph(List<(int, int, int)> graphData)
        //{
        //    Dictionary<int, Node> graph = new Dictionary<int, Node>();
        //    foreach ((int, int, int) row in graphData)
        //    {
        //        Node node = AddOrGetNode(graph, row.Item1); //откуда
        //        Node adjacentNode = AddOrGetNode(graph, row.Item2); //куда
        //        if (adjacentNode == null)
        //            continue;
        //        Edge edge = new Edge(adjacentNode, row.Item3);
        //        node.edges.Add(edge);
        //        adjacentNode.parents.Add(node, edge);
        //    }
        //    return graph;
        //}
        public bool ContainsNode(Point pos, Dictionary<int, Node> graph)
        {
            for (int i = 0; i < graph.Count; i++)
                if (AreNodesClose(graph.ElementAt(i).Value.position, pos, 10))
                    return true;
            return false;
        }
        public bool AreNodesClose(Point point1, Point point2, double radius)
        {
            double radiusSquared = radius * radius;
            double distanceSquared = (point1.X - point2.X) * (point1.X - point2.X) +
                                     (point1.Y - point2.Y) * (point1.Y - point2.Y);
            return distanceSquared <= radiusSquared;
        }
        private Edge ConvertFromDTO(EdgeDTO curEdge, List<NodeDTO> nodes)
        {
            Node adjacentNode = new Node();
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].value == curEdge.adjacentNodeValue)
                    adjacentNode = new Node(nodes[i].value, nodes[i].position, nodes[i].nodePic);

            Edge edge = new Edge(adjacentNode, curEdge.weight, curEdge.num, curEdge.edgePic);
            return edge;
        }
        public Node (NodeDTO curNode, List<NodeDTO> nodes)
        {
            MyValue = curNode.value;
            position = curNode.position;
            nodePic = curNode.nodePic;
            Node parent = new Node(curNode.value, curNode.position, curNode.nodePic);
            HashSet<Edge> edges = new HashSet<Edge>();
            for (int i = 0; i < curNode.edges.Count; i++)
                edges.Add(ConvertFromDTO(curNode.edges.ElementAt(i), nodes));
            for (int i = 0; i < edges.Count; i++)
                edges.ElementAt(i).adjacentNode.parents.Add(parent, edges.ElementAt(i));
            this.edges = edges;
        }
    }

    public class Edge
    {
        public Node adjacentNode; //узел, на который ведёт ребро
        public int weight;
        public EdgePicture edgePic;
        public int num;
        //public Edge(Node adjacentNode, int weight, int numEdge) { this.adjacentNode = adjacentNode; this.weight = weight; num = numEdge; }
        public Edge() { }
        public Edge(Node adjacentNode, int weight, int num, EdgePicture edgePicture) { this.adjacentNode = adjacentNode; this.weight = weight; edgePic = edgePicture; this.num = num; }
        public string ToString() { return weight.ToString() + ", " + adjacentNode.ToString(); }
        public bool AddEdge(List<(int, int, int)> graphData, Node node, Node adjacentNode, int weight, bool typeEdge, int numEdge, EdgePicture edgePic)
        {
            Edge edge = new Edge(adjacentNode, weight, numEdge, edgePic);

            if (AddSearchElement(graphData, node, adjacentNode, weight))
            {
                adjacentNode.parents.Add(node, edge);
                node.edges.Add(edge);
            }
            return true;
        }
        public bool AddSearchElement(List<(int, int, int)> graphData, Node node, Node adjacentNode, int weight)
        {
            int count = 0; int marker = -1;
            foreach ((int, int, int) row in graphData)
            {
                count++;
                if (row.Item1 == node.MyValue && row.Item2 == adjacentNode.MyValue)
                {
                    MessageBox.Show("Такое ребро уже существует!");
                    return false;
                }
                else if (row.Item1 == node.MyValue && row.Item2 == -1)
                    marker = count - 1;
            }
            if (marker != -1)
                graphData[marker] = ((node.MyValue, adjacentNode.MyValue, weight));
            else
                graphData.Add((node.MyValue, adjacentNode.MyValue, weight));
            return true;
        }
    }
    public class EdgePicture
    {
        public string TbEdge { get; set; }
        public string ColorEdge { get; set; }

        public EdgePicture(string tb, string color)
        {
            TbEdge = tb;
            ColorEdge = color;
        }
        public EdgePicture() { }
    }
    public class NodePicture
    {
        public string tbNode { get; set; }
        public string colorNode { get; set; }
        public NodePicture(string tb, string color)
        {
            tbNode = tb;
            colorNode = color;
        }
        public NodePicture() { }
    }
    public class EdgeDTO
    {
        public int adjacentNodeValue;
        public int weight;
        public EdgePicture edgePic;
        public int num;
        public EdgeDTO() { }
        public EdgeDTO(Edge edge)
        {
            adjacentNodeValue = edge.adjacentNode.MyValue;
            weight = edge.weight;
            edgePic = edge.edgePic;
            num = edge.num;
        }
        
    }
    public class NodeDTO
    {
        public int value;
        public Point position;
        public NodePicture nodePic;
        public HashSet<EdgeDTO> edges = new HashSet<EdgeDTO>();
        //public Dictionary<NodeDTO, EdgeDTO> parents = new Dictionary<NodeDTO, EdgeDTO>();
        public NodeDTO() { }
        public NodeDTO(Node node)
        {
            value = node.MyValue;
            position = node.position;
            nodePic = node.nodePic;
            for (int i = 0; i < node.edges.Count; i++)
                edges.Add(new EdgeDTO(node.edges.ElementAt(i)));
            //for (int j = 0; j < node.parents.Count; j++)
            //    parents.Add(node.parents.ElementAt(j).Key, new EdgeDTO(node.parents.ElementAt(j).Value));

        }
        
    }
}
