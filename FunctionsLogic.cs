using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WPFlab3
{
    public class FunctionsLogic
    {
        public int size = 20;
        public bool newVertex = false;
        public bool newEdge = false;
        private MainWindow mainWindow;
        public FunctionsLogic(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }
        public void CreateVertex(Point position, Node node)
        {
            string selectedColorName = GetSelectedColor();
            Brush strokeBrush = ConvertStringToBrush(node.nodePic.colorNode); //selectedColorName);
            Ellipse vertex = new Ellipse()
            {
                Width = size,
                Height = size,
                Stroke = strokeBrush,
                StrokeThickness = 2,
                Fill = Brushes.White,
            };
            vertex.MouseDown += mainWindow.PaintColor; //подписываем vertex на событие изменения цвета
            double posX = position.X - vertex.Width / 2;
            double posY = position.Y - vertex.Height / 2;

            TextBlock label = new TextBlock //создаем текстблок - номер вершины
            {
                Text = mainWindow.graph.Count().ToString(),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold,
            };

            // Установка привязки
            Binding binding = new Binding("MyValue")
            {
                Source = node,
                StringFormat = node.MyValue.ToString(), // Значение вершины в canvas
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };

            label.SetBinding(TextBlock.TextProperty, binding);

            var grid = new Grid(); //засовываем вершину и текстбокс в один объект grid
            grid.Children.Add(vertex);
            grid.Children.Add(label);

            Canvas.SetTop(grid, posY);
            Canvas.SetLeft(grid, posX);
            mainWindow.DrawingCanvas.Children.Add(grid);
        }
        private bool AddEdge(List<(int, int, int)> graphData, Node node, Node adjacentNode, int weight, Edge edge)
        {
            if (edge.AddSearchElement(graphData, node, adjacentNode, weight))
            {
                adjacentNode.parents.Add(node, edge);
                node.edges.Add(edge);
                return true;
            }
            else return false;

        }
        public void AddEdge(Point pos1, Point pos2, Dictionary<int, Node> graph, List<(int, int, int)> graphData, int weight, Edge paintEdge)
        {
            Node from = new Node(); Node to = new Node();
            for (int i = 0; i < graph.Count; i++) //если ребро между вершинами, то меняем координаты начала и конца ребра на координаты вершины, чтобы было по центру
            {
                if (from.AreNodesClose(pos1, graph.ElementAt(i).Value.position, 10))
                    from = graph.ElementAt(i).Value;
                else if (to.AreNodesClose(pos2, graph.ElementAt(i).Value.position, 10))
                    to = graph.ElementAt(i).Value;
            }
            int numEdges = CalculateEdges(graph);
            EdgePicture edgePic = new EdgePicture(numEdges.ToString(), "Black");
            Edge edge2 = new Edge(to, weight, (numEdges+1), edgePic); ;
            
            if (from.ContainsNode(from.position, graph) && to.ContainsNode(to.position, graph))
                CreateEdge(from, to, paintEdge, edge2, weight);
        }
        public void CreateEdge(Node from, Node to, Edge paintEdge, Edge edge2, int weight)
        {
            if (AddEdge(mainWindow.graphData, from, to, weight, edge2))
            {
                string selectedColorName = GetSelectedColor();
                Brush strokeBrush;
                if (paintEdge == null)
                    strokeBrush = ConvertStringToBrush(selectedColorName);
                else strokeBrush = ConvertStringToBrush(paintEdge.edgePic.ColorEdge);
                Line edge = new Line()
                {
                    X1 = from.position.X,
                    Y1 = from.position.Y,
                    X2 = to.position.X,
                    Y2 = to.position.Y,
                    Stroke = strokeBrush,
                    StrokeThickness = 2
                };
                edge.MouseDown += mainWindow.PaintColor;

                this.newEdge = false;
                TextBox textBox = new TextBox
                {
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0, 0, 0, 0),
                    Width = 60,
                    Height = 18,
                    IsReadOnly = false,
                    IsHitTestVisible = true,
                };
                string tb = "№ " + edge2.num.ToString();
                if (weight != 0)
                    tb += ";" + "Вес " + weight.ToString();
                textBox.Text = tb;
                textBox.IsEnabled = false;

                textBox.PreviewMouseDown += (object sender, MouseButtonEventArgs e) =>
                {
                    var clickedTb = sender as TextBox;
                    if (clickedTb != null)
                        clickedTb.Focus();
                };

                double centerX = (from.position.X + to.position.X) / 2;
                double centerY = (from.position.Y + to.position.Y) / 2;

                Canvas.SetLeft(textBox, centerX - textBox.ActualWidth / 2);
                Canvas.SetTop(textBox, centerY - textBox.ActualHeight / 2);
                edge.Tag = textBox;

                mainWindow.DrawingCanvas.Children.Add(edge);
                mainWindow.DrawingCanvas.Children.Add(textBox);
                if (mainWindow.isOriented == true)
                {
                    Polygon polygon = new Polygon();
                    polygon = DrawArrow(from.position, to.position);
                    textBox.Tag = polygon;
                    mainWindow.DrawingCanvas.Children.Add(polygon);
                }
            }
        }
        public Polygon DrawArrow(Point pos1, Point pos2)
        {
            double arrowLength = 10; // Длина стрелки
            double angle = Math.Atan2(pos2.Y - pos1.Y, pos2.X - pos1.X); // Угол в радианах

            Polygon arrowHead = new Polygon
            {
                Fill = Brushes.Black,
                Points = new PointCollection
                {
                    new Point(pos2.X, pos2.Y),
                    new Point(pos2.X - arrowLength * Math.Cos(angle - Math.PI / 6),
                    pos2.Y - arrowLength * Math.Sin(angle - Math.PI / 6)), // Левый угол стрелки

                    new Point(pos2.X - arrowLength * Math.Cos(angle + Math.PI / 6),
                    pos2.Y - arrowLength * Math.Sin(angle + Math.PI / 6))  // Правый угол стрелки
                }
            };
            return arrowHead;
        }
        public int[,] GenerateIncidenceMatrix(Dictionary<int, Node> graph)
        {
            int numEdges = CalculateEdges(graph);

            if (numEdges < 1)
                return null;
            int[,] matrix = new int[graph.Count + 1, numEdges];
            for (int i = 0; i < numEdges; i++)
            {
                matrix[0, i] = i + 1;
                for (int j = 1; j < graph.Count; j++)
                    matrix[j, i] = 0;
            }

            int edgeIndex = 0;
            foreach (var node in graph.Values)
            {
                foreach (var edge in node.edges)
                {
                    int rowIndex = node.MyValue;
                    int colIndex = edgeIndex++;

                    matrix[rowIndex + 1, colIndex] = edge.weight > 0 ? edge.weight : 1;
                    if (mainWindow.isOriented == true)
                        matrix[edge.adjacentNode.MyValue + 1, colIndex] = -(edge.weight > 0 ? edge.weight : 1);
                    else
                        matrix[edge.adjacentNode.MyValue + 1, colIndex] = edge.weight > 0 ? edge.weight : 1;
                }
            }
            return matrix;
        }
        private int CalculateEdges(Dictionary<int, Node> graph)
        {
            List<Edge> allEdges = new List<Edge>();
            foreach (var node in graph.Values)
                allEdges.AddRange(node.edges);
            return allEdges.Count;
        }
        public int[,] GenerateAdjacencyMatrix(Dictionary<int, Node> graph)
        {
            if (graph == null || graph.Count == 0)
                return null;
            int[,] matrix = new int[graph.Count + 1, graph.Count];
            for (int i = 0; i < graph.Count; i++)
            {
                matrix[0, i] = graph.ElementAt(i).Value.MyValue;
                for (int j = 1; j < graph.Count; j++)
                    matrix[i, j] = 0;
            }

            foreach (var nodePair in graph)
            {
                int rowIndex = nodePair.Key;
                Node node = nodePair.Value;

                foreach (Edge edge in node.edges)
                {
                    int colIndex = edge.adjacentNode.MyValue;
                    matrix[rowIndex + 1, colIndex] = edge.weight > 0 ? edge.weight : 1;
                    if (mainWindow.isOriented == false)
                        matrix[colIndex + 1, rowIndex] = edge.weight > 0 ? edge.weight : 1;
                }
            }
            return matrix;
        }

        public List<List<int>> SearchPath(int[,] AdjacencyMatrix, int startVertex, int endVertex)//alg Deikstra
        {
            int verticesCount = AdjacencyMatrix.GetLength(1);
            int[] distances = new int[verticesCount];
            bool[] visited = new bool[verticesCount];
            List<int>[] paths = new List<int>[verticesCount];
            List<List<int>> resultPaths = new List<List<int>>();

            // Инициализация
            for (int i = 0; i < verticesCount; i++)
            {
                distances[i] = int.MaxValue;
                visited[i] = false;
                paths[i] = new List<int>();
            }

            distances[startVertex] = 0;
            paths[startVertex].Add(startVertex);

            for (int count = 0; count < verticesCount - 1; count++)
            {
                int u = MinDistance(distances, visited);
                if (u == -1) break;
                visited[u] = true;

                for (int v = 0; v < verticesCount; v++)
                    if (!visited[v] && AdjacencyMatrix[u + 1, v] != 0 &&
                        distances[u] != int.MaxValue)
                    {
                        int newDist = distances[u] + AdjacencyMatrix[u + 1, v];
                        if (newDist < distances[v])
                        {
                            distances[v] = newDist;
                            paths[v] = new List<int>(paths[u]) { v };
                        }
                        else if (newDist == distances[v])
                        {
                            paths[v].AddRange(paths[u]);
                            paths[v].Add(v);
                        }
                    }
            }
            foreach (var path in paths[endVertex])
                if (path != endVertex)
                {
                    List<int> altPath = new List<int>(paths[path]) { endVertex };
                    if (!resultPaths.Contains(altPath))
                        resultPaths.Add(altPath);
                }
            return resultPaths;
        }
        private int MinDistance(int[] distances, bool[] shortestPathSet)
        {
            int min = int.MaxValue, minIndex = -1;
            for (int v = 0; v < distances.Length; v++)
                if (!shortestPathSet[v] && distances[v] <= min)
                {
                    min = distances[v];
                    minIndex = v;
                }
            return minIndex;
        }

        public string GetSelectedColor()
        {
            if (mainWindow.BlackButton.IsChecked == true)
                return "Black";
            if (mainWindow.RedButton.IsChecked == true)
                return "Red";
            if (mainWindow.OrangeButton.IsChecked == true)
                return "Orange";
            if (mainWindow.YellowButton.IsChecked == true)
                return "Yellow";
            if (mainWindow.GreenButton.IsChecked == true)
                return "Green";
            if (mainWindow.CadetBlueButton.IsChecked == true)
                return "CadetBlue";
            return "Black";
        }
        public Brush ConvertStringToBrush(string colorName)
        {
            var property = typeof(Brushes).GetProperty(colorName);
            if (property != null)
                return (Brush)property.GetValue(null);
            else
                return Brushes.Black;
        }
        public List<List<int>> SearchMaximumFlowProblem(List<List<int>> allPaths, Dictionary<int, Node> graph, int startVertex) //Ford-Falkerson alg
        {
            int maxFlowProblem = 0;
            int weight = int.MaxValue;
            List<int> path = new List<int>();
            for (int num = 0; num < allPaths.Count; num++)
            {
                path = allPaths[num];
                path.Insert(0, startVertex);

                for (int v = 0; v < path.Count - 1; v++)
                    for (int i = 0; i < graph[path[v]].edges.Count; i++)
                        if (v != path.Count - 1)
                            if (graph[path[v]].edges.ElementAt(i).adjacentNode.MyValue == path[v + 1] && weight > graph[path[v]].edges.ElementAt(i).weight)
                                weight = graph[path[v]].edges.ElementAt(i).weight;
                if (weight != int.MaxValue)
                    maxFlowProblem = maxFlowProblem + weight;
                weight = int.MaxValue;
            }
            mainWindow.tb_graph.Text += "Максимальный поток равен " + maxFlowProblem.ToString();
            return allPaths;
        }
        public List<List<int>> ExtractListsFromTextBox(string text) //слизал полностью из гпт, каюсь перед всеми высшими силами и низшими, что властны надо мной и сим миром, Аминь
        {
            List<List<int>> result = new List<List<int>>();
            // Разделяем текст на строки
            string[] lines = text.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            Array.Resize(ref lines, lines.Length - 1);
            foreach (var line in lines)
            {
                // Разделяем строку на числа, удаляем лишние пробелы и преобразуем в int
                List<int> numbers = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(num =>
                {
                    int.TryParse(num.Trim(), out int n);
                    return n; // Возвращаем число, если преобразование успешно
                }).ToList();
                // Добавляем список чисел в результирующий список
                result.Add(numbers);
            }
            return result != null && result.Count > 0 ? result : null;
        }
        public bool IsPointInsideEllipse(Grid grid, int x, int y)
        {
            double left = Canvas.GetLeft(grid);
            double top = Canvas.GetTop(grid);

            Ellipse ellipse = (Ellipse)grid.Children[0];
            double width = ellipse.Width;
            double height = ellipse.Height;

            double h = left + width / 2;
            double k = top + height / 2;
            double r = width / 2;

            return (Math.Pow(x - h, 2) / Math.Pow(r, 2)) + (Math.Pow(y - k, 2) / Math.Pow(r, 2)) <= 1;
        }
        public void FindRoutes(int[,] adjacencyMatrix, int start, int end, bool[] visited, string route, TextBox tb)
        {
            int allVertices = adjacencyMatrix.GetLength(1);
            if (start == end)
                tb.Text += route.Substring(0, route.Length - 2) + ";" + '\n';
            else
            {
                visited[start] = true;
                for (int i = 0; i < allVertices; i++)
                    if (adjacencyMatrix[start + 1, i] != 0 && !visited[i])
                        FindRoutes(adjacencyMatrix, i, end, visited, route + i + ", ", tb);
                visited[start] = false;
            }
        }
        public List<Edge> SearchMBST(int[,] adjacencyMatrix)
        {
            List<Edge> mbstEdges = new List<Edge>();
            HashSet<int> visited = new HashSet<int>();
            PriorityQueue<Edge, int> priorityQueue = new PriorityQueue<Edge, int>();
            int numberOfNodes = adjacencyMatrix.GetLength(1);

            // Начинаем с первого узла (индекс 0)
            int startNodeIndex = 0;
            visited.Add(startNodeIndex);

            // Добавляем все рёбра, исходящие из начального узла
            for (int j = 0; j < numberOfNodes; j++)
            {
                if (adjacencyMatrix[startNodeIndex+1, j] > 0) // Проверяем наличие ребра
                {
                    Edge edge = new Edge
                    {
                        adjacentNode = new Node { MyValue = j }, // Создаем новый узел
                        weight = adjacencyMatrix[startNodeIndex+1, j]
                    };
                    priorityQueue.Enqueue(edge, edge.weight);
                }
            }

            while (priorityQueue.Count > 0)
            {
                Edge edge = priorityQueue.Dequeue();

                // Если соседний узел уже посещен, пропускаем его
                if (visited.Contains(edge.adjacentNode.MyValue)) continue;

                mbstEdges.Add(edge);
                visited.Add(edge.adjacentNode.MyValue);

                // Добавляем все рёбра, исходящие из нового узла
                for (int j = 0; j < numberOfNodes; j++)
                {
                    if (adjacencyMatrix[edge.adjacentNode.MyValue+1, j] > 0 && !visited.Contains(j)) // Проверяем наличие ребра и посещенность
                    {
                        Edge nextEdge = new Edge
                        {
                            adjacentNode = new Node { MyValue = j }, // Создаем новый узел
                            weight = adjacencyMatrix[edge.adjacentNode.MyValue+1, j]
                        };

                        priorityQueue.Enqueue(nextEdge, nextEdge.weight);
                    }
                }
            }

            return mbstEdges;
        }
        public List<Edge> SearchMBST(Dictionary<int, Node> nodes) //alg Prima
        {
            List<Edge> mbstEdges = new List<Edge>();
            HashSet<Node> visited = new HashSet<Node>();
            PriorityQueue<Edge, int> priorityQueue = new PriorityQueue<Edge, int>();

            Node startNode = nodes.Values.First();
            visited.Add(startNode);
            foreach (var edge in startNode.edges)
                priorityQueue.Enqueue(edge, edge.weight);

            while (priorityQueue.Count > 0)
            {
                Edge edge = priorityQueue.Dequeue();
                if (visited.Contains(edge.adjacentNode))
                    continue;

                mbstEdges.Add(edge);
                visited.Add(edge.adjacentNode);
                foreach (var nextEdge in edge.adjacentNode.edges)
                    if (!visited.Contains(nextEdge.adjacentNode))
                        priorityQueue.Enqueue(nextEdge, nextEdge.weight);
            }
            return mbstEdges;
        }
        public bool IsPointOnLine(Line line, Point position, double a)
        {
            double x1 = line.X1;
            double y1 = line.Y1;
            double x2 = line.X2;
            double y2 = line.Y2;

            double distance = DistanceFromPointToLineSegment(x1, y1, x2, y2, position.X, position.Y); //расстояние от точки до линии
            return distance <= a;
        }

        private double DistanceFromPointToLineSegment(double x1, double y1, double x2, double y2, double px, double py)
        {
            double A = px - x1;
            double B = py - y1;
            double C = x2 - x1;
            double D = y2 - y1;

            double dot = A * C + B * D;
            double len_sq = C * C + D * D;
            double param = -1;
            if (len_sq != 0) // если линия не нулевая
                param = dot / len_sq;

            double xx, yy;
            if (param < 0) // ближайшая точка - начало отрезка
            {
                xx = x1;
                yy = y1;
            }
            else if (param > 1) // ближайшая точка - конец отрезка
            {
                xx = x2;
                yy = y2;
            }
            else // ближайшая точка - на отрезке
            {
                xx = x1 + param * C;
                yy = y1 + param * D;
            }

            double dx = px - xx;
            double dy = py - yy;
            return Math.Sqrt(dx * dx + dy * dy); //расстояние до ближайшей точки
        }
        public void CreateGraph(List<Node> nodes, Dictionary<int,Node> graph)
        {
            for (int count = 0; count < nodes.Count; count++)
            {
                CreateVertex(nodes[count].position, nodes[count]);
                if (nodes[count].edges != null)
                    for (int i = 0; i < nodes[count].edges.Count; i++)
                        CreateEdge(nodes[count], nodes[count].edges.ElementAt(i).adjacentNode, nodes[count].edges.ElementAt(i), nodes[count].edges.ElementAt(i), nodes[count].edges.ElementAt(i).weight);
            }
        }

    }
    class Vertex
    {
        public int Index { get; set; }
        public int Distance { get; set; }
        public List<int> Path { get; set; }
        public List<(int, int)> Edges = new List<(int, int)>(); //prev, current

        public Vertex(int index, int distance, List<int> path, List<(int, int)> edges)
        {
            Index = index;
            Distance = distance;
            Path = new List<int>(path);
            Edges = edges;
        }
    }
    public class EdgeMBST
    {
        public int Source { get; set; }
        public int Destination { get; set; }
        public int Weight { get; set; }
    }
}
