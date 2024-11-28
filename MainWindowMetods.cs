using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WPFlab3
{
    public class MainWindowMetods
    {
        private Canvas _canvas;
        private Dictionary<int, Node> _graph; // Определите свой тип графа
        private List<(int, int, int)> _graphData;
        private FunctionsLogic _function; // Вспомогательный класс для функций
        

        public MainWindowMetods(Canvas canvas, Dictionary<int, Node> graph, List<(int, int, int)> graphData, FunctionsLogic function)
        {
            _canvas = canvas;
            _graph = graph;
            _graphData = graphData;
            _function = function;
        }

        public void HandleNewVertex(Point position)
        {
            Node node = new Node();
            if (!node.ContainsNode(position, _graph))
            {
                node = node.AddOrGetNode(_graph, _graph.Count);
                node.position = position;
                NodePicture nodePic = new NodePicture(_graph.Count().ToString(), _function.GetSelectedColor());
                node.nodePic = nodePic;

                _function.CreateVertex(position, node);
                _graphData.Add((node.MyValue, -1, 0));
            }
        }

        public void HandleDelete(Point position)
        {
            if (_canvas != null && _canvas.InputHitTest(position) != null)
            {
                var element = _canvas.InputHitTest(position) as UIElement;

                if (element.GetType() == typeof(Ellipse))
                {
                    var temp = _canvas.InputHitTest(position) as UIElement;
                    while (temp != null && !(temp is Grid))
                        temp = VisualTreeHelper.GetParent(temp) as UIElement;
                    if (temp is Grid grid)
                        _canvas.Children.Remove(grid);


                    for (int i = 0; i < _graph.Count; i++)
                        if (_graph.ElementAt(i).Value.AreNodesClose(position, _graph.ElementAt(i).Value.position, _function.size / 2 + 10))
                        {
                            for (int j = 0; j < _graphData.Count; j++)
                            {
                                if (_graphData[j].Item2 == _graph.ElementAt(i).Value.MyValue)
                                    _graphData[j] = (_graphData[j].Item1, -1, _graphData[j].Item3);
                                if (_graphData[j].Item1 == _graph.ElementAt(i).Value.MyValue)
                                {
                                    _graphData.RemoveAt(j);
                                    break;
                                }
                            }
                            Node delNode = _graph.ElementAt(i).Value;
                            _graph.Remove(_graph.ElementAt(i).Key);
                            for (int k = 0; k < _graph.Count; k++)
                            {
                                for (int l = 0; l < delNode.parents.Count; l++)
                                    if (_graph.ElementAt(k).Value == delNode.parents.ElementAt(l).Key) //если вершина - предок
                                        _graph.ElementAt(k).Value.edges.Remove(delNode.parents.ElementAt(l).Value);
                                    else
                                        for (int j = 0; j < delNode.edges.Count; j++)
                                            if (_graph.ElementAt(k).Value == delNode.edges.ElementAt(j).adjacentNode) //если вершина - потомок  
                                                _graph.ElementAt(k).Value.parents.Remove(_graph.ElementAt(k).Value.parents.ElementAt(0).Key);
                            }

                            for (int k = delNode.MyValue; k < _graph.Count; k++)
                            {
                                int newV = _graphData[k].Item1 - 1;
                                _graphData[k] = (newV, _graphData[k].Item2, _graphData[k].Item3);

                                _graph.ElementAt(k).Value.MyValue--;
                                Node tempNode = _graph.ElementAt(k).Value;
                                _graph.Remove(_graph.ElementAt(k).Key);
                                _graph.Add(tempNode.MyValue, tempNode);
                            }

                            // + удаление картинки ребра
                            List<Line> delLines = new List<Line>();
                            if (delNode.parents.Count > 0)
                                for (int j = 0; j < delNode.parents.Count; j++)
                                {
                                    Line delLine = new Line
                                    {
                                        X1 = delNode.parents.ElementAt(j).Key.position.X,
                                        Y1 = delNode.parents.ElementAt(j).Key.position.Y,
                                        X2 = delNode.position.X,
                                        Y2 = delNode.position.Y,
                                    };
                                    delLines.Add(delLine);
                                }
                            if (delNode.edges.Count > 0)
                                for (int j = 0; j < delNode.edges.Count; j++)
                                {
                                    Line delLine = new Line
                                    {
                                        X1 = delNode.position.X,
                                        Y1 = delNode.position.Y,
                                        X2 = delNode.edges.ElementAt(j).adjacentNode.position.X,
                                        Y2 = delNode.edges.ElementAt(j).adjacentNode.position.Y,
                                    };
                                    delLines.Add(delLine);
                                }
                            for (int z = _canvas.Children.Count - 1; z >= 0; z--)
                                if (_canvas.Children[z] is Line line)
                                    for (int w = delLines.Count - 1; w >= 0; w--)
                                        if (delLines[w].X1 == line.X1 && delLines[w].X2 == line.X2 && delLines[w].Y1 == line.Y1 && delLines[w].Y2 == line.Y2)
                                        {
                                            delLines.RemoveAt(w);
                                            _canvas.Children.RemoveAt(z);
                                            TextBox tbToRemove = line.Tag as TextBox;
                                            Polygon arrowToRemove = tbToRemove.Tag as Polygon;
                                            _canvas.Children.Remove(tbToRemove);
                                            try
                                            {
                                                _canvas.Children.Remove(arrowToRemove);
                                            }
                                            catch { }
                                        }
                        }
                }
                if (element.GetType() == typeof(Line))
                {
                    _canvas.Children.Remove(element);
                    Line line = (Line)element;
                    TextBox tbToRemove = line.Tag as TextBox;
                    Polygon arrowToRemove = tbToRemove.Tag as Polygon;
                    _canvas.Children.Remove(tbToRemove);
                    try
                    {
                        _canvas.Children.Remove(arrowToRemove);
                    }
                    catch { }
                    System.Windows.Point begin = new System.Windows.Point(line.X1, line.Y1);
                    System.Windows.Point end = new System.Windows.Point(line.X2, line.Y2);
                    for (int i = 0; i < _graph.Count; i++)
                        if (_graph.ElementAt(i).Value.AreNodesClose(begin, _graph.ElementAt(i).Value.position, 10) || _graph.ElementAt(i).Value.AreNodesClose(end, _graph.ElementAt(i).Value.position, 10))
                        {
                            for (int j = 0; j < _graphData.Count; j++)
                                if (_graphData[j].Item2 == _graph.ElementAt(i).Value.MyValue)
                                {
                                    _graphData[j] = (_graphData[j].Item1, -1, _graphData[j].Item3);
                                    break;
                                }


                            if (_graph.ElementAt(i).Value.position == end)
                            {
                                for (int k = 0; k < _graph.Count; k++)
                                    for (int l = 0; l < _graph.ElementAt(i).Value.parents.Count; l++)
                                        if (begin == _graph.ElementAt(i).Value.parents.ElementAt(l).Key.position) //для конца ребра
                                            _graph.ElementAt(i).Value.parents.Remove(_graph.ElementAt(k).Value);
                            }
                            else if (_graph.ElementAt(i).Value.position == begin)
                                for (int j = 0; j < _graph.Count; j++)
                                {
                                    for (int k = 0; k < _graph[i].edges.Count; k++) //для начала ребра
                                        if (_graph.ElementAt(j).Value == _graph[i].edges.ElementAt(k).adjacentNode && _graph.ElementAt(j).Value.position == end)
                                            _graph.ElementAt(i).Value.edges.Remove(_graph[i].edges.ElementAt(k));
                                }
                        }
                }
            }
        }


        public void HandlePointer(Point position)
        {
            var el = _canvas.InputHitTest(position) as UIElement;
            NewWeight nw = new NewWeight();
            if (el.GetType() == typeof(Line))
            {
                Line line = (Line)el;
                nw.ShowDialog();
                int weight = nw.newWeight;
                for (int V = 0; V < _graph.Count; V++)
                {
                    for (int i = 0; i < _graph[i].edges.Count; i++)
                        if (_graph[V].edges.Count > 0 && _graph[V].edges != null)
                            if (_function.IsPointOnLine(line, _graph[V].position, 5) && _function.IsPointOnLine(line, _graph[V].edges.ElementAt(i).adjacentNode.position, 5))
                            {
                                _graph[V].edges.ElementAt(i).weight = weight;
                                TextBox tb = line.Tag as TextBox;
                                string[] parts = tb.Text.Split(';');
                                tb.Text = parts[0] + ";Вес " + weight.ToString();
                            }
                    for (int j = 0; j < _graph[V].parents.Count; j++)
                        if (_graph[V].parents.Count > 0 && _graph[V].parents != null)
                            if (_function.IsPointOnLine(line, _graph[V].parents.ElementAt(j).Value.adjacentNode.position, 5) && _function.IsPointOnLine(line, _graph[V].position, 5))
                                _graph[V].parents.ElementAt(j).Value.weight = weight;
                }
            }
        }
    }
}