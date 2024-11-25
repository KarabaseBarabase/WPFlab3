using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPFlab3
{
    public partial class MainWindow : Window
    {

        private FunctionsLogic function;
        private System.Windows.Point MousePos;
        private bool newVertex = false;
        private bool newEdge = false;
        private bool delete = false;
        private bool pointer = false;
        public bool isOriented;
        public List<(int, int, int)> graphData = new List<(int, int, int)>(); // (int,int,int,Point)
        public Dictionary<int, Node> graph = new Dictionary<int, Node>();
        private Line tempLine = new Line();
        public MainWindow()
        {
            function = new FunctionsLogic(this);
            InitializeComponent();
            tb_save.Text = AppDomain.CurrentDomain.BaseDirectory + "\\userSettings.json";
            tb_load.Text = AppDomain.CurrentDomain.BaseDirectory + "\\userSettings.json";
        }
        public void PaintColor(object sender, RoutedEventArgs e)
        {
            bool isChecked = Bucket.IsChecked ?? false;
            if (isChecked == true)
            {
                if (sender is Line line)
                {
                    string selectedColorName = function.GetSelectedColor();
                    line.Stroke = function.ConvertStringToBrush(selectedColorName);
                    for (int V = 0; V < graph.Count; V++)
                    {
                        for (int i = 0; i < graph[V].edges.Count; i++)
                            if (graph[V].edges.Count > 0 && graph[V].edges != null)
                                if (function.IsPointOnLine(line, graph[V].position, 5) && function.IsPointOnLine(line, graph[V].edges.ElementAt(i).adjacentNode.position, 5))
                                    graph[V].edges.ElementAt(i).edgePic.ColorEdge = selectedColorName;
                        for (int j = 0; j < graph[V].parents.Count; j++)
                            if (graph[V].parents.Count > 0 && graph[V].parents != null)
                                if (function.IsPointOnLine(line, graph[V].parents.ElementAt(j).Key.position, 5) && function.IsPointOnLine(line, graph[V].position, 5))
                                    graph[V].parents.ElementAt(j).Value.edgePic.ColorEdge = selectedColorName;
                    }
                }
                else if (sender is Ellipse vertex)
                {
                    Grid thisGrid = new Grid();
                    for (int i = 0; i < DrawingCanvas.Children.Count; i++)
                        if (DrawingCanvas.Children[i] is Grid grid && grid.Children[0] == vertex)
                        {
                            thisGrid = grid; break;
                        }

                    string selectedColorName = function.GetSelectedColor();
                    vertex.Stroke = function.ConvertStringToBrush(selectedColorName);
                    for (int V = 0; V < graph.Count; V++)
                        if (function.IsPointInsideEllipse(thisGrid, Convert.ToInt32(graph[V].position.X), Convert.ToInt32(graph[V].position.Y)))
                            graph[V].nodePic.colorNode = selectedColorName;
                }
            }
        }
        public void MouseLeftBtnDown_DrawingGraph(object sender, MouseButtonEventArgs e)
        {
            MousePos = e.GetPosition(DrawingCanvas);
            if (newVertex)
            {
                Node node = new Node();
                if (!node.ContainsNode(MousePos, graph))
                {
                    node = node.AddOrGetNode(graph, graph.Count);
                    node.position = MousePos;
                    function.CreateVertex(MousePos, node);
                    graphData.Add((node.MyValue, -1, 0));

                    NodePicture nodePic = new NodePicture(graph.Count().ToString(), "Black");
                    node.nodePic = nodePic;
                }
            }
            else if (newEdge)
            {
                tempLine = new Line
                {
                    X1 = MousePos.X,
                    Y1 = MousePos.Y,
                    X2 = MousePos.X,
                    Y2 = MousePos.Y,
                    Stroke = function.ConvertStringToBrush(function.GetSelectedColor()),
                    StrokeThickness = 2,
                };
                DrawingCanvas.Children.Add(tempLine);
            }
            else if (delete)
            {
                if (DrawingCanvas != null && DrawingCanvas.InputHitTest(MousePos) != null)
                {
                    var element = DrawingCanvas.InputHitTest(MousePos) as UIElement;

                    if (element.GetType() == typeof(Ellipse))
                    {
                        var temp = DrawingCanvas.InputHitTest(MousePos) as UIElement;
                        while (temp != null && !(temp is Grid))
                            temp = VisualTreeHelper.GetParent(temp) as UIElement;
                        if (temp is Grid grid)
                            DrawingCanvas.Children.Remove(grid);


                        for (int i = 0; i < graph.Count; i++)
                            if (graph.ElementAt(i).Value.AreNodesClose(MousePos, graph.ElementAt(i).Value.position, function.size / 2 + 10))
                            {
                                for (int j = 0; j < graphData.Count; j++)
                                {
                                    if (graphData[j].Item2 == graph.ElementAt(i).Value.MyValue)
                                        graphData[j] = (graphData[j].Item1, -1, graphData[j].Item3);
                                    if (graphData[j].Item1 == graph.ElementAt(i).Value.MyValue)
                                    {
                                        graphData.RemoveAt(j);
                                        break;
                                    }
                                }
                                Node delNode = graph.ElementAt(i).Value;
                                graph.Remove(graph.ElementAt(i).Key);
                                for (int k = 0; k < graph.Count; k++)
                                {
                                    for (int l = 0; l < delNode.parents.Count; l++)
                                        if (graph.ElementAt(k).Value == delNode.parents.ElementAt(l).Key) //если вершина - предок
                                            graph.ElementAt(k).Value.edges.Remove(delNode.parents.ElementAt(l).Value);
                                        else
                                            for (int j = 0; j < delNode.edges.Count; j++)
                                                if (graph.ElementAt(k).Value == delNode.edges.ElementAt(j).adjacentNode) //если вершина - потомок  
                                                    graph.ElementAt(k).Value.parents.Remove(graph.ElementAt(k).Value.parents.ElementAt(0).Key);
                                }

                                for (int k = delNode.MyValue; k < graph.Count; k++)
                                {
                                    int newV = graphData[k].Item1 - 1;
                                    graphData[k] = (newV, graphData[k].Item2, graphData[k].Item3);

                                    graph.ElementAt(k).Value.MyValue--;
                                    Node tempNode = graph.ElementAt(k).Value;
                                    graph.Remove(graph.ElementAt(k).Key);
                                    graph.Add(tempNode.MyValue, tempNode);
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
                                for (int z = DrawingCanvas.Children.Count - 1; z >= 0; z--)
                                    if (DrawingCanvas.Children[z] is Line line)
                                        for (int w = delLines.Count - 1; w >= 0; w--)
                                            if (delLines[w].X1 == line.X1 && delLines[w].X2 == line.X2 && delLines[w].Y1 == line.Y1 && delLines[w].Y2 == line.Y2)
                                            {
                                                delLines.RemoveAt(w);
                                                DrawingCanvas.Children.RemoveAt(z);
                                                TextBox tbToRemove = line.Tag as TextBox;
                                                Polygon arrowToRemove = tbToRemove.Tag as Polygon;
                                                DrawingCanvas.Children.Remove(tbToRemove);
                                                try
                                                {
                                                    DrawingCanvas.Children.Remove(arrowToRemove);
                                                }
                                                catch { }
                                            }
                            }
                    }
                    if (element.GetType() == typeof(Line))
                    {
                        DrawingCanvas.Children.Remove(element);
                        Line line = (Line)element;
                        TextBox tbToRemove = line.Tag as TextBox;
                        Polygon arrowToRemove = tbToRemove.Tag as Polygon;
                        DrawingCanvas.Children.Remove(tbToRemove);
                        try
                        {
                            DrawingCanvas.Children.Remove(arrowToRemove);
                        }
                        catch { }
                        System.Windows.Point begin = new System.Windows.Point(line.X1, line.Y1);
                        System.Windows.Point end = new System.Windows.Point(line.X2, line.Y2);
                        for (int i = 0; i < graph.Count; i++)
                            if (graph.ElementAt(i).Value.AreNodesClose(begin, graph.ElementAt(i).Value.position, 10) || graph.ElementAt(i).Value.AreNodesClose(end, graph.ElementAt(i).Value.position, 10))
                            {
                                for (int j = 0; j < graphData.Count; j++)
                                    if (graphData[j].Item2 == graph.ElementAt(i).Value.MyValue)
                                    {
                                        graphData[j] = (graphData[j].Item1, -1, graphData[j].Item3);
                                        break;
                                    }


                                if (graph.ElementAt(i).Value.position == end)
                                {
                                    for (int k = 0; k < graph.Count; k++)
                                        for (int l = 0; l < graph.ElementAt(i).Value.parents.Count; l++)
                                            if (begin == graph.ElementAt(i).Value.parents.ElementAt(l).Key.position) //для конца ребра
                                                graph.ElementAt(i).Value.parents.Remove(graph.ElementAt(k).Value);
                                }
                                else if (graph.ElementAt(i).Value.position == begin)
                                    for (int j = 0; j < graph.Count; j++)
                                    {
                                        for (int k = 0; k < graph[i].edges.Count; k++) //для начала ребра
                                            if (graph.ElementAt(j).Value == graph[i].edges.ElementAt(k).adjacentNode && graph.ElementAt(j).Value.position == end)
                                                graph.ElementAt(i).Value.edges.Remove(graph[i].edges.ElementAt(k));
                                    }
                            }
                    }
                }
            }
            else if (pointer)
            {
                var el = DrawingCanvas.InputHitTest(MousePos) as UIElement;
                NewWeight nw = new NewWeight();
                if (el.GetType() == typeof(Line))
                {
                    Line line = (Line)el;
                    nw.ShowDialog();
                    int weight = nw.newWeight;
                    for (int V = 0; V < graph.Count; V++)
                    {
                        for (int i = 0; i < graph[i].edges.Count; i++)
                            if (graph[V].edges.Count > 0 && graph[V].edges != null)
                                if (function.IsPointOnLine(line, graph[V].position, 5) && function.IsPointOnLine(line, graph[V].edges.ElementAt(i).adjacentNode.position, 5))
                                {
                                    graph[V].edges.ElementAt(i).weight = weight;
                                    TextBox tb = line.Tag as TextBox;
                                    string[] parts = tb.Text.Split(';');
                                    tb.Text = parts[0] + ";Вес " + weight.ToString();
                                }
                        for (int j = 0; j < graph[V].parents.Count; j++)
                            if (graph[V].parents.Count > 0 && graph[V].parents != null)
                                if (function.IsPointOnLine(line, graph[V].parents.ElementAt(j).Value.adjacentNode.position, 5) && function.IsPointOnLine(line, graph[V].position, 5))
                                    graph[V].parents.ElementAt(j).Value.weight = weight;
                    }
                }
            }
        }

        private void MouseLeftButtonUp_DrawingGraph(object sender, MouseButtonEventArgs e) //for add edge
        {
            if (newEdge && tempLine != null)
            {
                DrawingCanvas.Children.Remove(tempLine);
                System.Windows.Point secondMousePos = e.GetPosition(DrawingCanvas);
                tempLine.X2 = secondMousePos.X;
                tempLine.Y2 = secondMousePos.Y;
                tempLine = null;
                if (string.IsNullOrEmpty(tbWeight.Text))
                    tbWeight.Text = "0";
                function.AddEdge(MousePos, secondMousePos, graph, graphData, Convert.ToInt32(tbWeight.Text));
            }
        }
        private void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (tempLine != null)
            {
                System.Windows.Point currentPoint = e.GetPosition(DrawingCanvas);
                tempLine.X2 = currentPoint.X;
                tempLine.Y2 = currentPoint.Y;
            }
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            var checkedButton = sender as ToggleButton;
            ResetToggleButtons(colors1, checkedButton);
            ResetToggleButtons(colors2, checkedButton);

            foreach (var child in (checkedButton.Parent as Panel).Children)
                if (child is ToggleButton button && button != checkedButton)
                    button.IsChecked = false;
        }

        private void ControlToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            var checkedButton = sender as ToggleButton;

            if (checkedButton == null)
                return;
            ResetToggleButtons(DockPanel1, checkedButton);
            ResetToggleButtons(DockPanel3, checkedButton);
            ResetToggleButtons(DockPanel5, checkedButton);
            if (checkedButton == Pointer)
                pointer = true;
            else if (checkedButton == Vertex)
                newVertex = true;
            else if (checkedButton == Edge)
                newEdge = true;
            else if (checkedButton == Crest)
                delete = true;
            else if (checkedButton == Bucket)
                PaintColor(sender, e);
        }

        public void ResetToggleButtons(Panel panel, ToggleButton checkedButton)
        {
            foreach (var child in panel.Children)
                if (child is ToggleButton button && button != checkedButton)
                    button.IsChecked = false;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton pressed = (RadioButton)sender;

            if (pressed.Content.ToString() == "Ориентированный")
                isOriented = true;
            else
                isOriented = false;
        }

        public void BtnClick_GenerateIncidenceMatrix(object sender, RoutedEventArgs e)
        {
            int[,] arr = function.GenerateIncidenceMatrix(graph);
            if (arr == null) { tb_graph.Text = "Граф не имеет ребёр!"; return; }
            tb_graph.Text = "р/в".PadRight(arr.GetLength(1));
            for (int row = 0; row < arr.GetLength(0); row++)
            {
                if (row != 0)
                    tb_graph.Text += (row - 1).ToString().PadRight(arr.GetLength(0));
                for (int col = 0; col < arr.GetLength(1); col++)
                    tb_graph.Text += arr[row, col].ToString().PadRight(arr.GetLength(0));
                tb_graph.Text += '\n';
            }
        }
        public void BtnClick_GenerateAdjacencyMatrix(object sender, EventArgs e)
        {
            int[,] arr = function.GenerateAdjacencyMatrix(graph);
            if (arr == null) { tb_graph.Text = "Граф не имеет вершин!"; return; }
            tb_graph.Text = "в/в".PadRight(arr.GetLength(1));
            for (int row = 0; row < arr.GetLength(0); row++)
            {
                for (int col = 0; col < arr.GetLength(1); col++)
                    tb_graph.Text += arr[row, col].ToString().PadRight(arr.GetLength(0));
                tb_graph.Text += '\n';
                if (row != arr.GetLength(0) - 1)
                    tb_graph.Text += arr[0, row].ToString().PadRight(arr.GetLength(0));
            }
        }
        public void BtnClick_SearchShortestPath(object sender, RoutedEventArgs e)
        {
            ResetColour(graph);

            InputWindow iw = new InputWindow();
            int start = 0; int end = 0;
            iw.ShowDialog();
            if (iw.isOpen == false)
            {
                start = iw.GetStartV();
                end = iw.GetEndV();
                if (end == start)
                    return;
            }

            List<List<int>> allPaths = function.SearchPath(function.GenerateAdjacencyMatrix(graph), start, end);
            if (allPaths == null || allPaths.Count == 0)
            {
                tb_graph.Text = "Граф не имеет путей из вершины " + start.ToString() + " в " + end.ToString();
                return;
            }
            else { tb_graph.Text = "Кратчайший путь из вершины " + start.ToString() + " в " + end.ToString() + " найден."; }

            for (int k = 0; k < allPaths.Count; k++)
            {
                List<int> path = allPaths[k];
                if (path != null && path.Count > 0)
                    for (int i = 0; i < DrawingCanvas.Children.Count; i++)
                        if (DrawingCanvas.Children[i] is Grid grid)
                        {
                            Ellipse ellipse = (Ellipse)grid.Children[0];
                            for (int j = 0; j < path.Count; j++)
                                if (function.IsPointInsideEllipse(grid, Convert.ToInt32(graph.ElementAt(path[j]).Value.position.X), Convert.ToInt32(graph.ElementAt(path[j]).Value.position.Y)))
                                    ellipse.Fill = Brushes.Blue;
                        }
            }
        }

        public void BtnClick_SearchMaximumFlowProblem(object sender, RoutedEventArgs e)
        {
            ResetColour(graph);
            tb_graph.Clear();
            InputWindow iw = new InputWindow();
            int start = 0; int end = 0;
            iw.ShowDialog();
            if (iw.isOpen == false)
            {
                start = iw.GetStartV();
                end = iw.GetEndV();
                if (end == start)
                    return;
            }

            bool[] visited = new bool[graph.Count];
            function.FindRoutes(function.GenerateAdjacencyMatrix(graph), start, end, visited, "", tb_graph);
            List<List<int>> allPaths = function.ExtractListsFromTextBox(tb_graph.Text);
            if (allPaths == null) { MessageBox.Show("Нет доступных путей из вершины А в B"); return; }

            function.SearchMaximumFlowProblem(allPaths, graph, start);
            for (int k = 0; k < allPaths.Count; k++)
            {
                List<int> curPath = allPaths[k];
                curPath.Insert(0, start);
                if (curPath != null && curPath.Count > 0)
                    for (int i = 0; i < DrawingCanvas.Children.Count; i++)
                        if (DrawingCanvas.Children[i] is Grid grid)
                        {
                            Ellipse ellipse = (Ellipse)grid.Children[0];
                            for (int j = 0; j < curPath.Count; j++)
                                if (function.IsPointInsideEllipse(grid, Convert.ToInt32(graph.ElementAt(curPath[j]).Value.position.X), Convert.ToInt32(graph.ElementAt(curPath[j]).Value.position.Y)))
                                    ellipse.Fill = Brushes.Blue;
                        }
            }
        }
        public void BtnClick_SearchMBST(object sender, RoutedEventArgs e)
        {
            ResetColour(graph);
            tb_graph.Clear();

            List<Edge> mbstEdges = function.SearchMBST(graph);
            if (mbstEdges.Count == 0 || mbstEdges == null)
            {
                tb_graph.Text = "Минимальное остовное дерево не найдено. Скорее всего, ваш граф не связный.";
                return;
            }
            else { tb_graph.Text = "Минимальное покрывающее дерево найдено."; }

            bool[] visited = new bool[graph.Count];
            for (int obj = 0; obj < DrawingCanvas.Children.Count; obj++)
                if (DrawingCanvas.Children[obj] is Line line)
                    for (int edg = 0; edg < mbstEdges.Count; edg++)
                        if (function.IsPointOnLine(line, mbstEdges[edg].adjacentNode.position, 5))
                            for (int j = 0; j < mbstEdges[edg].adjacentNode.parents.Count; j++)
                                if (function.IsPointOnLine(line, mbstEdges[edg].adjacentNode.parents.ElementAt(j).Key.position, 5))
                                {
                                    if (visited[mbstEdges[edg].adjacentNode.MyValue] != true)
                                        visited[mbstEdges[edg].adjacentNode.MyValue] = true;
                                    else break;
                                    line.Stroke = Brushes.Blue;
                                }
        }
        public void ResetColour(Dictionary<int, Node> graph)
        {
            for (int V = 0; V < graph.Count; V++)
            {
                if (graph.ElementAt(V).Value.nodePic.colorNode == "Blue")
                    graph.ElementAt(V).Value.nodePic.colorNode = "White";
                for (int i = 0; i < graph.ElementAt(V).Value.edges.Count; i++)
                    if (graph.ElementAt(V).Value.edges.ElementAt(i).edgePic.ColorEdge == "Blue")
                        graph.ElementAt(V).Value.edges.ElementAt(i).edgePic.ColorEdge = "Black";
                for (int j = 0; j < graph.ElementAt(V).Value.parents.Count; j++)
                    if (graph.ElementAt(V).Value.parents.ElementAt(j).Value.edgePic.ColorEdge == "Blue")
                        graph.ElementAt(V).Value.parents.ElementAt(j).Value.edgePic.ColorEdge = "Black";
            }
            for (int i = 0; i < DrawingCanvas.Children.Count; i++)
                if (DrawingCanvas.Children[i] is Grid grid)
                {
                    Ellipse ellipse = (Ellipse)grid.Children[0];
                    if (ellipse.Fill == Brushes.Blue)
                        ellipse.Fill = Brushes.White;
                }
        }
        private void ControlToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            pointer = false;
            newEdge = false;
            newVertex = false;
            delete = false;
        }

        private void edgeSelector_TextChanged(object sender, TextChangedEventArgs e)
        {
            //int newWeight = 0;
            //int index = Convert.ToInt32(EdgeSelector.Text);
            //try
            //{
            //    newWeight = Convert.ToInt32(WeightChange.Text);
            //}
            //catch { }
            //if (newWeight > 0)
            //{
            //    function.edgePictures.ElementAt(index).Key.weight = Convert.ToInt32(WeightChange);
            //}
        }
        private void BtnClick_SaveGraph(object sender, RoutedEventArgs e)
        {
            SettingsManager settingsManager = new SettingsManager();
            List<NodeDTO> graphDTO = new List<NodeDTO>();

            for (int i = 0; i < graph.Count; i++)
                graphDTO.Add(new NodeDTO(graph.ElementAt(i).Value));
            settingsManager.SaveSettings(graphDTO, tb_save.Text);
            MessageBox.Show("Граф успешно сохранён.");
        }
        private void BtnClick_LoadGraph(object sender, RoutedEventArgs e)
        {
            SettingsManager settingsManager = new SettingsManager();
            List<NodeDTO> nodesDTO = settingsManager.LoadSettings(tb_save.Text);
            Dictionary<int, Node> newGraph = new Dictionary<int, Node>();
            DrawingCanvas.Children.Clear(); graphData.Clear();

            for (int i = 0; i < nodesDTO.Count; i++)
            {
                Node node = new Node(nodesDTO[i], nodesDTO);
                MessageBox.Show(node.ToString());
                newGraph.Add(nodesDTO[i].value, node);
            }
            graph = newGraph;
            function.CreateGraph(newGraph.Values.ToList(), graph);
        }
        
    }
}
